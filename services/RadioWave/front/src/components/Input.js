import React from 'react'
import './Input.css'

const Input = ({field}) => {
    return <label className={"Input-label"} htmlFor={field.name}>{field.name}: <input className={"Input"} {...field} /></label>
}

export default Input