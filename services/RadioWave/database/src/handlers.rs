use std::sync::Arc;
use std::io::Read;
use iron::{status, AfterMiddleware, Handler, IronResult, Request, Response};
use iron::headers::ContentType;
use rustc_serialize::json;
use database::Database;
use router::Router;
use models::Message;
use std::error::Error;

macro_rules! try_it {
    ($e:expr) => {
        match $e {
            Ok(x) => x,
            Err(e) => return Ok(Response::with((status::InternalServerError, e.description())))
        }
    };
    ($e:expr, $error:expr) => {
        match $e {
            Ok(x) => x,
            Err(e) => return Ok(Response::with(($error, e.description())))
        }
    }
}

macro_rules! get_http_param {
    ($r:expr, $e:expr) => {
        match $r.extensions.get::<Router>() {
            Some(router) => {
                match router.find($e) {
                    Some(v) => v,
                    None => return Ok(Response::with(status::BadRequest)),
                }
            },
            None => return Ok(Response::with(status::InternalServerError))
        }
    }
}

pub struct Handlers {
    pub post_msg: PostMessageHandler,
    pub get_msg: GetMessageHandler,
}

impl Handlers {
    pub fn new(database: Arc<Database>) -> Handlers {
        Handlers {
            post_msg: PostMessageHandler { database: database.clone() },
            get_msg: GetMessageHandler { database: database.clone() },
        }
    }
}

pub struct PostMessageHandler {
    database: Arc<Database>,
}

impl Handler for PostMessageHandler {
    fn handle(&self, req: &mut Request) -> IronResult<Response> {
        let ref key = get_http_param!(req, "key");
        let mut payload = String::new();
        try_it!(req.body.read_to_string(&mut payload));

        let msg: Message = try_it!(json::decode(&payload), status::BadRequest);

        if !is_valid_str(&key.to_string()) | !msg.is_valid() {
            return Ok(Response::with(status::BadRequest));
        }

        let result = &self.database.add(key.to_string(), msg);

        Ok(Response::with((status::Ok, try_it!(json::encode(result)))))
    }
}

pub struct GetMessageHandler {
    database: Arc<Database>,
}

impl Handler for GetMessageHandler {
    fn handle(&self, req: &mut Request) -> IronResult<Response> {
        let ref key = get_http_param!(req, "key");
        if !is_valid_str(&key.to_string()) {
            return Ok(Response::with(status::BadRequest));
        }
        match self.database.get(&key.to_string()) {
            Some(msg) => Ok(Response::with((status::Ok, try_it!(json::encode(&msg))))),
            None => Ok(Response::with((status::Ok, "[]")))
        }
    }
}

pub struct JsonAfterMiddleware;

impl AfterMiddleware for JsonAfterMiddleware {
    fn after(&self, _: &mut Request, mut res: Response) -> IronResult<Response> {
        res.headers.set(ContentType::json());
        Ok(res)
    }
}


fn is_valid_str(string: &String) -> bool {
    string.len() < 100
}
