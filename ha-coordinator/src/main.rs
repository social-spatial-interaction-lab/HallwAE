mod models;

use actix_web::{web, App, HttpResponse, HttpServer, Responder};
use log::{info, warn};
use models::{LobbyInfo, QuickJoinResponse, RegisterLobbyRequest};
use std::collections::HashMap;
use std::sync::Mutex;
use std::time::{SystemTime, UNIX_EPOCH};

// Store active lobbies and their states
struct AppState {
    lobbies: Mutex<HashMap<String, LobbyInfo>>,
    lobby_creation_lock: Mutex<Option<SystemTime>>, // Lock for lobby creation
}

const CREATION_LOCK_TIMEOUT: u64 = 5; // 5 seconds timeout for creation lock

async fn quick_join(data: web::Data<AppState>) -> impl Responder {
    info!("Quick join request received");

    let mut lobbies = data.lobbies.lock().unwrap();
    info!("Current active lobbies: {}", lobbies.len());

    // First, look for available lobbies
    if let Some(lobby) = lobbies.values_mut().next() {
        lobby.player_count += 1; // Increment player count immediately
        info!(
            "Found existing lobby. ID: {}, Players: {}/{}",
            lobby.lobby_id, lobby.player_count, lobby.max_players
        );

        return HttpResponse::Ok().json(QuickJoinResponse {
            lobby_id: Some(lobby.lobby_id.clone()),
            join_code: Some(lobby.join_code.clone()),
            should_create: false,
            creation_token: 0,
        });
    }

    info!("No available lobbies found, checking creation lock");

    // No lobbies found, handle creation permission
    let mut creation_lock = data.lobby_creation_lock.lock().unwrap();

    // Check if there's an active creation lock
    if let Some(lock_time) = *creation_lock {
        let now = SystemTime::now();
        let lock_duration = now.duration_since(lock_time).unwrap().as_secs();

        if lock_duration < CREATION_LOCK_TIMEOUT {
            info!(
                "Creation lock active for {}s, telling client to wait",
                lock_duration
            );
            return HttpResponse::Ok().json(QuickJoinResponse {
                lobby_id: None,
                join_code: None,
                should_create: false,
                creation_token: 0,
            });
        }
        info!("Creation lock expired after {}s", lock_duration);
    }

    // Grant creation permission to this client
    let now = SystemTime::now();
    *creation_lock = Some(now);
    let creation_token = now.duration_since(UNIX_EPOCH).unwrap().as_secs();

    info!(
        "Granting creation permission with token: {}",
        creation_token
    );

    HttpResponse::Ok().json(QuickJoinResponse {
        lobby_id: None,
        join_code: None,
        should_create: true,
        creation_token: creation_token,
    })
}

async fn register_lobby(
    data: web::Data<AppState>,
    req: web::Json<RegisterLobbyRequest>,
) -> impl Responder {
    let req_inner = req.into_inner();
    info!(
        "Register lobby request - ID: {}, Join Code: {}",
        req_inner.lobby_id, req_inner.join_code
    );

    let mut creation_lock = data.lobby_creation_lock.lock().unwrap();

    // Verify creation token
    if let Some(lock_time) = *creation_lock {
        let token = lock_time.duration_since(UNIX_EPOCH).unwrap().as_secs();
        if token != req_inner.creation_token {
            warn!(
                "Invalid creation token: {} (expected {})",
                req_inner.creation_token, token
            );
            return HttpResponse::BadRequest().json("Invalid creation token");
        }
    } else {
        warn!("No active creation permission");
        return HttpResponse::BadRequest().json("No active creation permission");
    }

    // Register the new lobby
    let mut lobbies = data.lobbies.lock().unwrap();
    lobbies.insert(
        req_inner.lobby_id.clone(),
        LobbyInfo {
            lobby_id: req_inner.lobby_id.clone(),
            join_code: req_inner.join_code.clone(),
            player_count: 1,
            max_players: req_inner.max_players,
        },
    );

    info!("Registered new lobby. Total lobbies: {}", lobbies.len());

    // Clear creation lock
    *creation_lock = None;
    info!("Creation lock cleared");

    HttpResponse::Ok().json("Lobby registered successfully")
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    // Initialize logger with timestamp
    env_logger::Builder::from_default_env()
        .format_timestamp_millis()
        .init();

    info!("Starting Lobby Coordinator Server");

    let app_state = web::Data::new(AppState {
        lobbies: Mutex::new(HashMap::new()),
        lobby_creation_lock: Mutex::new(None),
    });

    info!("State initialized");
    info!("Binding to 0.0.0.0:8111");

    HttpServer::new(move || {
        App::new()
            .app_data(app_state.clone())
            .route("/quick_join", web::post().to(quick_join))
            .route("/register_lobby", web::post().to(register_lobby))
    })
    .bind("0.0.0.0:8111")?
    .run()
    .await
}
