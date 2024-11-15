use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Clone)]
pub struct LobbyInfo {
    pub lobby_id: String,
    pub join_code: String,
    pub player_count: u32,
    pub max_players: u32,
}

#[derive(Deserialize)]
pub struct QuickJoinRequest {
    pub build_id: String,
    pub max_players: u32,
}

#[derive(Serialize)]
pub struct QuickJoinResponse {
    pub lobby_id: Option<String>,
    pub join_code: Option<String>,
    pub should_create: bool,
    pub creation_token: Option<u64>,
}

#[derive(Deserialize)]
pub struct RegisterLobbyRequest {
    pub lobby_id: String,
    pub join_code: String,
    pub max_players: u32,
    pub creation_token: u64,
} 