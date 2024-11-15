mod models;

use actix_web::{web, App, HttpResponse, HttpServer, Responder};
use std::sync::Mutex;
use std::collections::HashMap;
use std::time::{SystemTime, UNIX_EPOCH};
use models::{LobbyInfo, QuickJoinRequest, QuickJoinResponse, RegisterLobbyRequest};

// Store active lobbies and their states
struct AppState {
    lobbies: Mutex<HashMap<String, LobbyInfo>>,
    lobby_creation_lock: Mutex<Option<SystemTime>>, // Lock for lobby creation
}

const CREATION_LOCK_TIMEOUT: u64 = 5; // 5 seconds timeout for creation lock

async fn quick_join(
    data: web::Data<AppState>,
    req: web::Json<QuickJoinRequest>,
) -> impl Responder {
    let mut lobbies = data.lobbies.lock().unwrap();
    
    // First, look for available lobbies
    for (_, lobby) in lobbies.iter_mut() {
        if lobby.scene_name == req.scene_name {
            lobby.player_count += 1;  // Increment player count immediately
            return HttpResponse::Ok().json(QuickJoinResponse {
                lobby_id: Some(lobby.lobby_id.clone()),
                join_code: Some(lobby.join_code.clone()),
                should_create: false,
                creation_token: None,
            });
        }
    }

    // No lobbies found, handle creation permission
    let mut creation_lock = data.lobby_creation_lock.lock().unwrap();
    
    // Check if there's an active creation lock
    if let Some(lock_time) = *creation_lock {
        let now = SystemTime::now();
        if now.duration_since(lock_time).unwrap().as_secs() < CREATION_LOCK_TIMEOUT {
            // Someone else is creating a lobby, tell client to wait and retry
            return HttpResponse::Ok().json(QuickJoinResponse {
                lobby_id: None,
                join_code: None,
                should_create: false,
                creation_token: None,
            });
        }
    }

    // Grant creation permission to this client
    let now = SystemTime::now();
    *creation_lock = Some(now);
    let creation_token = now.duration_since(UNIX_EPOCH).unwrap().as_secs();

    HttpResponse::Ok().json(QuickJoinResponse {
        lobby_id: None,
        join_code: None,
        should_create: true,
        creation_token: Some(creation_token),
    })
}

async fn register_lobby(
    data: web::Data<AppState>,
    req: web::Json<RegisterLobbyRequest>,
) -> impl Responder {
    let mut creation_lock = data.lobby_creation_lock.lock().unwrap();
    
    // Verify creation token
    if let Some(lock_time) = *creation_lock {
        let token = lock_time.duration_since(UNIX_EPOCH).unwrap().as_secs();
        if token != req.creation_token {
            return HttpResponse::BadRequest().json("Invalid creation token");
        }
    } else {
        return HttpResponse::BadRequest().json("No active creation permission");
    }

    // Register the new lobby
    let mut lobbies = data.lobbies.lock().unwrap();
    lobbies.insert(req.lobby_id.clone(), LobbyInfo {
        lobby_id: req.lobby_id.clone(),
        join_code: req.join_code.clone(),
        player_count: 1,
        max_players: req.max_players,
        scene_name: req.scene_name.clone(),
    });

    // Clear creation lock
    *creation_lock = None;

    HttpResponse::Ok().json("Lobby registered successfully")
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    let app_state = web::Data::new(AppState {
        lobbies: Mutex::new(HashMap::new()),
        lobby_creation_lock: Mutex::new(None),
    });

    HttpServer::new(move || {
        App::new()
            .app_data(app_state.clone())
            .route("/quick_join", web::post().to(quick_join))
            .route("/register_lobby", web::post().to(register_lobby))
    })
    .bind("127.0.0.1:8080")?
    .run()
    .await
}
