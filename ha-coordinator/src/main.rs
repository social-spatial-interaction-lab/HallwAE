use actix_web::{web, App, HttpResponse, HttpServer, Responder};
use serde::{Deserialize, Serialize};
use std::sync::Mutex;
use std::collections::HashMap;

// Store active lobbies and their states
struct AppState {
    lobbies: Mutex<HashMap<String, LobbyInfo>>,
}

#[derive(Serialize, Deserialize, Clone)]
struct LobbyInfo {
    lobby_id: String,
    join_code: String,
    player_count: i32,
    max_players: i32,
    scene_name: String,
    is_locked: bool, // Used to prevent race conditions during join operations
}

#[derive(Deserialize)]
struct QuickJoinRequest {
    scene_name: String,
    build_id: String,
    max_players: i32,
}

#[derive(Serialize)]
struct QuickJoinResponse {
    lobby_id: Option<String>,
    join_code: Option<String>,
    should_create: bool,
}

async fn quick_join(
    data: web::Data<AppState>,
    req: web::Json<QuickJoinRequest>,
) -> impl Responder {
    let mut lobbies = data.lobbies.lock().unwrap();
    
    // Look for available lobbies in the same scene
    for (_, lobby) in lobbies.iter_mut() {
        if !lobby.is_locked 
            && lobby.scene_name == req.scene_name 
            && lobby.player_count < lobby.max_players 
        {
            // Lock the lobby while client attempts to join
            lobby.is_locked = true;
            
            return HttpResponse::Ok().json(QuickJoinResponse {
                lobby_id: Some(lobby.lobby_id.clone()),
                join_code: Some(lobby.join_code.clone()),
                should_create: false,
            });
        }
    }

    // No suitable lobby found, tell client to create new one
    HttpResponse::Ok().json(QuickJoinResponse {
        lobby_id: None,
        join_code: None,
        should_create: true,
    })
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    let app_state = web::Data::new(AppState {
        lobbies: Mutex::new(HashMap::new()),
    });

    HttpServer::new(move || {
        App::new()
            .app_data(app_state.clone())
            .route("/quick_join", web::post().to(quick_join))
    })
    .bind("127.0.0.1:8080")?
    .run()
    .await
}
