use models::Message;
use ws::Sender;
use chashmap::CHashMap;
use std::collections::HashMap;
use chrono::offset::utc::UTC;
use time::Duration;
use std::time::Instant;
use std::ops::Deref;
use rustc_serialize::json;

use std::fs::{OpenOptions, File, rename, remove_file, create_dir};
use std::io::prelude::*;
use std::io::BufReader;
use std::sync::{Arc, Mutex};

pub const SNAPSHOT_FILE: &'static str = "Snapshot/snapshot";
pub const TAIL_FILE: &'static str = "Snapshot/tail";

pub const A: i32 = 32;


#[derive(Clone)]
pub struct Database {
    database: CHashMap<String, Vec<Message>>,
    ws_broadcaster: Sender,
    ttl: Duration,
    lock_obj: Arc<Mutex<i32>>,
}

#[derive(RustcEncodable, RustcDecodable)]
struct SnapshotPair {
    key: String,
    value: Vec<Message>,
}

#[derive(RustcEncodable, RustcDecodable)]
struct TailPair {
    key: String,
    value: Message,
}

impl Database {
    pub fn new(ttl: u64, broadcaster: Sender) -> Database {
        create_dir("Snapshot").is_ok();
        let db = Database {
            database: CHashMap::new(),
            ws_broadcaster: broadcaster,
            ttl: Duration::seconds(ttl as i64),
            lock_obj: Arc::new(Mutex::new(A)),
        };

        let snapshot = SNAPSHOT_FILE.to_string();
        let tail = TAIL_FILE.to_string();

        if let Ok(file) = File::open(&snapshot) {
            info!("Loading from snapshot");
            for (_, line) in BufReader::new(file).lines().enumerate() {
                let pair: SnapshotPair = json::decode(line.unwrap().as_str()).unwrap();
                db.database.insert(pair.key, pair.value);
            }
        }

        let mut hashmap = HashMap::<String, Message>::new();

        if let Ok(file) = File::open(&(tail.clone() + ".old")) {
            info!("Loading from tail.old");
            for (_, line) in BufReader::new(file).lines().enumerate() {
                let pair: TailPair = json::decode(line.unwrap().as_str()).unwrap();
                hashmap.insert(pair.key, pair.value);
            }
        }

        if let Ok(file) = File::open(&tail) {
            info!("Loading from tail");
            for (_, line) in BufReader::new(file).lines().enumerate() {
                let pair: TailPair = json::decode(line.unwrap().as_str()).unwrap();
                hashmap.insert(pair.key, pair.value);
            }
        }
        else {
            File::create(&tail).unwrap();
        }

        for (key, value) in hashmap.into_iter() {
            if !db.database.contains_key(&key) {
                db.database.insert(key.clone(), Vec::new());
            }

            db.database.get_mut(&key).unwrap().push(value);
        }

        info!("Loaded {} keys", db.database.len());
        db
    }

    pub fn add(&self, key: String, mut value: Message) -> Vec<Message> {
        if !self.database.contains_key(&key) {
            self.database.insert(key.clone(), Vec::new());
            if !value.is_private {
                info!("Broadcasting '{}'", &key);
                match self.ws_broadcaster.broadcast(key.clone()) {
                    Ok(_) => (),
                    Err(e) => error!("{:?}", e)
                }
            }
        }

        let mut vec = self.database.get_mut(&key).unwrap();
        if vec.len() != 0 {
            let first = vec.get(0).unwrap();
            if !(first.password.is_none()) & !(value.password.is_none()) {
                if first.password != value.password {
                    return Vec::new();
                }
            }
        }

        value.prepare();
        
        {
            let lock = self.lock_obj.lock().unwrap();

            writeln!(append_file(TAIL_FILE), "{}",
                     json::encode(&TailPair { key, value: value.clone() }).unwrap());
            
            drop(lock);
        }
        
        vec.push(value);

        let mut result = vec.deref().clone();
        result.iter_mut().for_each(|x| x.datetime = None);

        result
    }

    pub fn get(&self, k: &String) -> Option<Vec<Message>> {
        match self.database.get(k) {
            Some(v) => {
                let mut result = v.deref().clone();
                result.iter_mut().for_each(|x| x.datetime = None);

                Some(result)
            }
            None => None
        }
    }

    pub fn clear(&self) {
        let now = Instant::now();
        let del_time = UTC::now() - self.ttl;
        let mut del_count = 0;

        self.database.clone().into_iter()
            .map(|(k, _)| k)
            .for_each(|key| {
                let mut v = self.database.get_mut(&key).unwrap();
                let len = v.len();

                v.retain(|msg| msg.datetime.unwrap() >= del_time);

                del_count += len - v.len();
            });

        self.database.retain(|_, v| v.len() != 0);
        self.database.shrink_to_fit();

        let then = now.elapsed();
        let elapsed = then.as_secs() * 1000 + then.subsec_nanos() as u64 / 1_000_000;
        info!("CleanUp! elapsed: {}ms, msg deleted: {}", elapsed, del_count);
    }

    pub fn snapshot(&self) {
        info!("Start snapshot");
        let snapshot = SNAPSHOT_FILE.to_string();
        let tail = TAIL_FILE.to_string();
        
        {
            let lock = self.lock_obj.lock().unwrap();
            
            rename(&tail, &(tail.clone() + ".old")).unwrap();
            create_file(&tail);
            
            drop(lock);
        }

        let mut file = create_file(&(snapshot.clone() + ".tmp"));
       
        self.database.clone().into_iter()
            .map(|(k, v)| json::encode(&SnapshotPair { key: k, value: v }).unwrap())
            .for_each(|json| { writeln!(file, "{}", json); });
        
        rename(snapshot.clone() + ".tmp", &snapshot).is_ok();
        remove_file(tail.clone() + ".old").is_ok();
        
        info!("Finish snapshot");
    }
}

fn create_file(name: &String) -> File {
    OpenOptions::new().write(true).create(true)
        .open(name).unwrap()
}

fn append_file(name: &str) -> File {
    OpenOptions::new().append(true).open(name.to_string()).unwrap()
}