import React, { Component } from "react";
import logo from "./logo.svg";
import Input from "./components/Input";
import "./App.css";
import { Formik, Form, Field } from "formik";

const host = window.location.host;


const initialState = {
  text: "SOS",
  DpM: 500,
  freq: 1000,
  ch: "RUCTFE",
  password: "",
};

let currentChanel = initialState.ch;

class App extends Component {
  constructor(props) {
    super(props);
    this.state = {
      news: ["RUCTFE", "RUCTFE", "RUCTFE", "RUCTFE", "RUCTFE"]
    };
    this.tryConnectRadio(initialState.ch);
    this.tryConnectNews();
  }

  tryConnectRadio = (ch = initialState.ch) => {
    try {
      const radioSocket = new WebSocket(`ws://${host}/radio/${ch}`);
      radioSocket.binaryType = "arraybuffer";
      radioSocket.onmessage = this.handleRadioMsg;
     
      radioSocket.onerror = () => {
        if (ch == currentChanel) {
          setTimeout(this.tryConnectRadio, 1000)
        }
          
      };
      radioSocket.onclose = () => {
        if (ch == currentChanel) {
          setTimeout(this.tryConnectRadio, 1000)
        }
      };
      

      this.radioSocket = radioSocket;
    } catch (e) {
      console.log(`💩: ${e.message}`);
    }
  };

  handleRadioMsg = ({ data }) => {
    try {
      const arr = this.convert(data);
      const Ctx = window.AudioContext || window.webkitAudioContext;
      const audioContext = new Ctx();
      const buffer = audioContext.createBuffer(1, arr.length, 8000);
      buffer.copyToChannel(arr, 0, 0);
      const source = audioContext.createBufferSource();
      source.buffer = buffer;
      source.connect(audioContext.destination);
      source.start();
    } catch (e) {
      console.log(`💩: ${e.message}`);
    }
  };

  convert = data => {
    const us = new Uint8Array(data);
    const fs = new Float32Array(us.length);
    for (let i = 0; i < us.length; i++) {
      fs[i] = us[i] / 128 - 1;
    }
    return fs;
  };

  changeChannel = e => {
    if (this.radioSocket.readyState === WebSocket.OPEN)
      this.radioSocket.close();
    this.tryConnectRadio(e.target.value);
    currentChanel = e.target.value;
  };

  tryConnectNews = () => {
    try {
      const newsSocket = new WebSocket(`ws://${host}/news`);
      newsSocket.onmessage = this.handleNewsMsg;
      newsSocket.onerror = () => setTimeout(this.tryConnectNews, 1000);
      newsSocket.onclose = () => setTimeout(this.tryConnectNews, 1000);
      this.newsSocket = newsSocket;
    } catch (e) {
      console.log(`💩: ${e.message}`);
    }
  };

  handleNewsMsg = ({ data }) => {
    try {
      this.append(data);
    } catch (e) {
      console.log(`💩: ${e.message}`);
    }
  };

  append = text => {
    const news = this.state.news.slice();
    news.push(text.toUpperCase());
    if (news.length > 5) {
      news.shift();
    }
    this.setState({ news });
  };

  render() {
    const { news } = this.state;
    return (
      <div className="App">
        <header className="App-header">
          <img src={logo} className="App-logo" alt="logo" />
          <Formik initialValues={initialState} onSubmit={this.submitNewMessage}>
            {() => (
              <Form className={"App-form"}>
                <Field name="text" component={Input} />
                <Field name="DpM" component={Input} />
                <Field name="freq" component={Input} />
                <Field
                  onChangeChannel={this.changeChannel}
                  name="ch"
                  component={Input}
                />
                <Field name="password" type="password" component={Input} />
                <button className="App-btn" type="submit">
                  send
                </button>
              </Form>
            )}
          </Formik>
          <section className={"App-news"}>
            {news.map((t, i) => (
              <div className={"App-text"} key={i}>
                {t.toUpperCase()}
              </div>
            ))}
          </section>
        </header>
      </div>
    );
  }

  submitNewMessage = values =>
    fetch(`http://${host}/db/${values.ch}`, {
      method: "post",
      mode: "no-cors",
      body: JSON.stringify({
        dpm: values.DpM,
        frequency: values.freq,
        text: values.text.toUpperCase(),
        need_base32: false,
        is_private: false,
        password: values.password,
      })
    });
}

export default App;
