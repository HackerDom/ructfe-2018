import React from "react";
import "./Input.css";

const Input = ({ field, onChangeChannel, form }) => {
  const onChange = (...args) => {
    if (onChangeChannel) {
      onChangeChannel(...args);
    }
    form.handleChange(...args);
  };
  return (
    <label className={"Input-label"} htmlFor={field.name}>
      {field.name}: <input className={"Input"} {...field} onChange={onChange} />
    </label>
  );
};
export default Input;
