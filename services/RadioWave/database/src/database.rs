use models::Message;
use ws::Sender;
use chashmap::CHashMap;
use chrono::offset::utc::UTC;
use time::Duration;
use std::time::Instant;
use std::ops::Deref;

#[derive(Clone)]
pub struct Database {
    database: CHashMap<String, Vec<Message>>,
    ws_broadcaster: Sender,
    cleanup_duration: Duration,
}

impl Database {
    pub fn new(cleanup_in_secs: u64, broadcaster: Sender) -> Database {
        Database {
            database: CHashMap::new(),
            ws_broadcaster: broadcaster,
            cleanup_duration: Duration::seconds(cleanup_in_secs as i64),
        }
    }

    pub fn add(&self, key: String, mut value: Message) -> Vec<Message> {
        if !self.database.contains_key(&key) {
            self.database.insert(key.clone(), Vec::new());
            if !value.is_private {
                info!("sending '{}' to all!", key.clone());
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
        info!("keys before {}", self.database.len());
        let now = Instant::now();
        let del_time = UTC::now() - self.cleanup_duration;
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
        info!("keys after {}", self.database.len());
    }
}