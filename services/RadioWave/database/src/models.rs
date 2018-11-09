use chrono::datetime::DateTime;
use chrono::offset::utc::UTC;
use base32;


#[derive(Clone, Debug, RustcEncodable, RustcDecodable)]
pub struct Message {
    dpm: i32,
    frequency: i32,
    need_base32: bool,
    pub text: String,
    pub password: Option<String>,
    pub datetime: Option<DateTime<UTC>>,
    pub is_private: bool,
}

impl Message {
    pub fn prepare(&mut self) {
        self.datetime = Some(UTC::now());

        if self.need_base32 {
            self.text = base32::encode(
                base32::Alphabet::RFC4648 { padding: false },
                self.text.as_bytes(),
            )
        }
    }
    
    pub fn is_valid(&self) -> bool {
        let valid_text = self.text.len() < 100;
        match &self.password { 
            Some(p) => valid_text & (p.len() < 100),
            None => valid_text
        }
    }
}
