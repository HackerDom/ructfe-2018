extern crate chrono;
extern crate env_logger;
extern crate iron;
extern crate logger;
extern crate router;
extern crate rustc_serialize;
extern crate chashmap;
extern crate time;
extern crate base32;

mod handlers;
mod models;
mod database;

use handlers::*;
use iron::prelude::Chain;
use iron::Iron;
use router::Router;
use logger::Logger;
use std::sync::Arc;
use std::thread;

fn main() {
    env_logger::init().unwrap();
    let (logger_before, logger_after) = Logger::new(None);

    let (port, cleanup_in_secs) = load_settings();
    
    let db = Arc::new(database::Database::new(cleanup_in_secs));
    let handlers = Handlers::new(db.clone());

    let mut router = Router::new();
    router.get("/search/:key", handlers.get_msg, "get_msg");
    router.post("/msg", handlers.post_msg, "post_msg");

    let mut chain = Chain::new(router);
    chain.link_before(logger_before);
    chain.link_after(logger_after);
    chain.link_after(JsonAfterMiddleware);

    thread::spawn(move || {
        loop {
            thread::sleep(std::time::Duration::from_secs(cleanup_in_secs));
            db.clear();
        }
    });

    Iron::new(chain).http(format!("localhost:{}", port)).unwrap();
}

#[derive(RustcDecodable)]
struct Settings {
    port: i32,
    cleanup_in_secs: u64,
}

fn load_settings() -> (i32, u64) {
    use std::fs::File;
    use std::io::prelude::*;
    use rustc_serialize::json;

    let mut file = File::open("settings.json").expect("file not found");
    let mut content = String::new();
    file.read_to_string(&mut content).unwrap();
    
    let settings: Settings = json::decode(content.as_str()).expect("Invalid settings file");
    (settings.port, settings.cleanup_in_secs)
}