extern crate chrono;
extern crate iron;
extern crate router;
extern crate rustc_serialize;
extern crate chashmap;
extern crate time;
extern crate base32;
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


fn main() {
    setup_logger();
    let (host, cleanup_in_secs, threads) = load_settings();

    let db = Arc::new(database::Database::new(cleanup_in_secs));
    let handlers = Handlers::new(db.clone());

    let mut router = Router::new();
    router.get("/search/:key", handlers.get_msg, "get_msg");
    router.post("/msg", handlers.post_msg, "post_msg");

    let mut chain = Chain::new(router);
    chain.link_after(JsonAfterMiddleware);

    thread::spawn(move || {
        loop {
            thread::sleep(std::time::Duration::from_secs(cleanup_in_secs));
            db.clear();
        }
    });
    
    info!("Listening to {}", host);
    Iron::new(chain)
        .listen_with(host, threads, Protocol::Http, None)
        .unwrap();
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

#[derive(RustcDecodable)]
struct Settings {
    host: String,
    cleanup_in_secs: u64,
    threads: usize,
}

fn load_settings() -> (String, u64, usize) {
    use std::fs::File;
    use std::io::prelude::*;
    use rustc_serialize::json;

    let mut file = File::open("settings.json").expect("settings file not found");
    let mut content = String::new();
    file.read_to_string(&mut content).unwrap();

    let settings: Settings = json::decode(content.as_str()).expect("Invalid settings file");
    (settings.host, settings.cleanup_in_secs, settings.threads)
}