use chrono::datetime::DateTime;
use chrono::offset::utc::UTC;
use base32;


#[derive(Clone, Debug, RustcEncodable, RustcDecodable)]
pub struct MessageOut {
    frequency: i32,
    dpm: i32,
    text: String,
    datetime: Option<DateTime<UTC>>,
}

impl MessageOut {
    pub fn from_msg_in(msg: &mut MessageIn) -> MessageOut {
        MessageOut {
            text: msg.text(),
            frequency: msg.frequency,
            dpm: msg.dpm,
            datetime: Some(UTC::now()),
        }
    }

    pub fn datetime(&self) -> DateTime<UTC> {
        self.datetime.unwrap()
    }

    pub fn clear_datetime(&mut self) {
        self.datetime = None;
    }
}

#[derive(Clone, Debug, RustcEncodable, RustcDecodable)]
pub struct MessageIn {
    key: String,
    frequency: i32,
    dpm: i32,
    need_base32: bool,
    text: String,
}

impl MessageIn {
    pub fn key(&self) -> &String {
        &self.key
    }

    pub fn text(&self) -> String {
        if self.need_base32 {
            return base32::encode(
                base32::Alphabet::RFC4648 { padding: false },
                self.text.as_bytes(),
            )
        }

        self.text.to_owned()
    }
}