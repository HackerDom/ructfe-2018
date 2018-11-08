extern crate chrono;
extern crate iron;
extern crate router;
extern crate rustc_serialize;
extern crate chashmap;
extern crate time;
extern crate base32;
extern crate ws;
extern crate simplelog;
#[macro_use]
extern crate log;


mod handlers;
mod models;
mod database;

use handlers::*;
use iron::prelude::Chain;
use iron::{Iron, Protocol};
use router::Router;
use std::sync::Arc;
use std::thread;
use database::Database;


fn main() {
    setup_logger();
    let settings = load_settings();
    
    let ws = ws::WebSocket::new(|_| move |_| Ok(())).unwrap();
    let db = Arc::new(Database::new(settings.ttl_sec, ws.broadcaster()));
    let handlers = Handlers::new(db.clone());

    let mut router = Router::new();
    router.get("/:key", handlers.get_msg, "get_msg");
    router.post("/:key", handlers.post_msg, "post_msg");

    let mut chain = Chain::new(router);
    chain.link_after(JsonAfterMiddleware);

    let cleanup_sec = settings.cleanup_sec;
    let db1 = db.clone();
    thread::spawn(move || {
        loop {
            thread::sleep(std::time::Duration::from_secs(cleanup_sec));
            db1.clear();
        }
    });

    let snapshot_sec = settings.snapshot_sec;
    let db2 = db.clone();
    thread::spawn(move || {
        loop {
            thread::sleep(std::time::Duration::from_secs(snapshot_sec));
            db2.snapshot();
        }
    });

    let ws_host = settings.ws_host;
    thread::spawn(move || {
        info!("WS broadcasting at '{}'", &ws_host);
        ws.bind(ws_host).unwrap().run().unwrap();
    });
    
    let db_host = settings.db_host;
    info!("HTTP listen at '{}'", &db_host);
    Iron::new(chain)
        .listen_with(db_host, settings.threads, Protocol::Http, None)
        .unwrap();
}

#[derive(RustcDecodable, Clone)]
struct Settings {
    pub db_host: String,
    pub ws_host: String,
    pub cleanup_sec: u64,
    pub ttl_sec: u64,
    pub snapshot_sec: u64,
    pub threads: usize,
}

fn load_settings() -> Settings {
    use std::fs::File;
    use std::io::prelude::*;
    use rustc_serialize::json;

    let mut file = File::open("settings.json").expect("settings file not found");
    let mut content = String::new();
    file.read_to_string(&mut content).unwrap();

    json::decode(content.as_str()).expect("Invalid settings file")
}

fn setup_logger() {
    use simplelog::*;
    use std::fs::File;
    CombinedLogger::init(
        vec![
            WriteLogger::new(LevelFilter::Info, Config::default(),
                             File::create("database.log").unwrap()),
        ]
    ).unwrap();
}