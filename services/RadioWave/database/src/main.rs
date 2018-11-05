extern crate chrono;
extern crate iron;
extern crate router;
extern crate rustc_serialize;
extern crate chashmap;
extern crate time;
extern crate base32;
#[macro_use] extern crate log;
extern crate simplelog;


mod handlers;
mod models;
mod database;

use handlers::*;
use iron::prelude::Chain;
use iron::Iron;
use router::Router;
use std::sync::Arc;
use std::thread;

fn main() {
    let (host, cleanup_in_secs) = load_settings();
    
    {
        use simplelog::*;
        use std::fs::File;
        CombinedLogger::init(
            vec![
                WriteLogger::new(LevelFilter::Info, Config::default(), 
                                 File::create("database.log").unwrap()),
                ]
        ).unwrap();
    }
    
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
    Iron::new(chain).http(host).unwrap();
}

#[derive(RustcDecodable)]
struct Settings {
    host: String,
    cleanup_in_secs: u64,
}

fn load_settings() -> (String, u64) {
    use std::fs::File;
    use std::io::prelude::*;
    use rustc_serialize::json;

    let mut file = File::open("settings.json").expect("settings file not found");
    let mut content = String::new();
    file.read_to_string(&mut content).unwrap();
    
    let settings: Settings = json::decode(content.as_str()).expect("Invalid settings file");
    (settings.host, settings.cleanup_in_secs)
}