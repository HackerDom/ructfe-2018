use models::MessageOut;
use chashmap::CHashMap;
use chrono::offset::utc::UTC;
use std::time::Instant;
use time::Duration;
use std::ops::Deref;

#[derive(Clone)]
pub struct Database {
    database: CHashMap<String, Vec<MessageOut>>,
    cleanup_duration: Duration
}

impl Database {
    pub fn new(cleanup_in_secs: u64) -> Database {
        Database { 
            database: CHashMap::new(),
            cleanup_duration: Duration::seconds(cleanup_in_secs as i64)
        }
    }

    pub fn add(&self, k: String, v: MessageOut) {
        if !self.database.contains_key(&k){
            self.database.insert(k.clone(), Vec::new());
        }
        
        self.database.get_mut(&k).unwrap().push(v);
    }

    pub fn get(&self, k: &String) -> Option<Vec<MessageOut>> {
        match self.database.get(k) {
            Some(v) => {
                let mut result = v.deref().to_owned();
                for x in result.iter_mut() {
                    x.clear_datetime();
                }

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
                
                v.retain(|msg| msg.datetime() >= del_time);
                
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