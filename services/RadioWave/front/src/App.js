import React, { Component } from "react";
import logo from "./logo.svg";
import Input from "./components/Input";
import "./App.css";
import { Formik, Form, Field } from "formik";
const server = "10.33.81.15:6455";

const InitialState = {
  text: "AAAA",
  DpM: 70,
  freq: 4000,
  ch: "1"
};

class App extends Component {
  constructor(props) {
    super(props);
    const radioSocket = new WebSocket(
      `ws://${server}/radio/${InitialState.ch}`
    );
    radioSocket.binaryType = "arraybuffer";
    radioSocket.onmessage = this.handleRadioMsg;
    this.radioSocket = radioSocket;
  }

  changeChannel = (ch) => {
      this.radioSocket.close()
      const radioSocket = new WebSocket(
          `ws://${server}/radio/${ch}`
      );
      radioSocket.binaryType = "arraybuffer";
      radioSocket.onmessage = this.handleRadioMsg;
      this.radioSocket = radioSocket;
  }

  handleRadioMsg = ({ data }) => {
    try {
        const audioContext = new window.AudioContext();
        const b = audioContext.createBuffer(1, data.byteLength, 8000);
        b.copyToChannel(new Float32Array(data), 0, 0)
        const s = audioContext.createBufferSource();
        s.buffer = b
        s.start()
    } catch (e) {
      console.log(`💩: ${e.message}`);
    }
  };

  render() {
    return (
      <div className="App">
        <header className="App-header">
          <img src={logo} className="App-logo" alt="logo" />
          <Formik initialValues={InitialState} onSubmit={this.submitNewMessage}>
            {() => (
              <Form className={"App-form"}>
                <Field name="text" component={Input} />
                <Field name="DpM" component={Input} />
                <Field name="freq" component={Input} />
                <Field name="ch" component={Input} />
                <button type="submit">send</button>
              </Form>
            )}
          </Formik>
        </header>
      </div>
    );
  }

  submitNewMessage = values => {
    fetch(`http://${server}/db/${values.ch}`, {
      method: "post",
      body: JSON.stringify({
        dpm: values.DpM,
        frequency: values.freq,
        text: values.text,
        need_base32: false,
        is_private: false
      })
    });
  };
}

export default App;
