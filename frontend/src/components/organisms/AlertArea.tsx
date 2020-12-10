import {
  Collapse,
  IconButton,
} from '@material-ui/core'
import Alert from '@material-ui/lab/Alert';
import { CloseIcon } from '@material-ui/data-grid';
import { useEffect } from 'react';
import React from 'react';

export interface AlertAreaProps{
  successText?: string;
  warningText?: string;
  errorText?: string;
  onHideSuccessText?: ()=>void;
  onHideWarningText?: ()=>void;
  onHideErrorText?: ()=>void;
}

const AlertArea = (props: AlertAreaProps) =>{
  const _successText = props.successText;
  const _warningText = props.warningText;
  const _errorText = props.errorText;

  const _onHideSuccessText = props.onHideSuccessText;
  const _onHideWarningText = props.onHideWarningText;
  const _onHideErrorText = props.onHideErrorText;

  const [alertErrorOpen, setAlertErrorOpen] = React.useState(!_errorText && _errorText !== "");
  const [alertWarningOpen, setAlertWarningOpen] = React.useState(!_warningText && _warningText !== "");
  const [alertSuccessOpen, setAlertSuccessOpen] = React.useState(!_successText && _successText !== "");

  useEffect(()=>{
    if(_successText && _successText !== ""){
      setAlertSuccessOpen(true);
    } else {
      setAlertSuccessOpen(false);
    }
  },[_successText]);
  useEffect(()=>{
    if(_warningText && _warningText !== ""){
      setAlertWarningOpen(true);
    }else{
      setAlertWarningOpen(false);
    }
  },[_warningText]);
  useEffect(()=>{
    if(_errorText && _errorText !== ""){
      setAlertErrorOpen(true);
    } else {
      setAlertErrorOpen(false);
    }
  },[_errorText]);

  useEffect(()=>{
    if(alertErrorOpen) return;
    const f = async ()=>{
      if(_onHideErrorText){
        _onHideErrorText();
      }
    }

    f();

  },[alertErrorOpen, _onHideErrorText]);

  useEffect(()=>{
    if(alertWarningOpen) return;
    const f = async ()=>{
      if(_onHideWarningText){
        _onHideWarningText();
      }
    }

    f();

  },[alertWarningOpen, _onHideWarningText]);

  useEffect(()=>{
    if(alertSuccessOpen) return;
    const f = async ()=>{
      if(_onHideSuccessText){
        _onHideSuccessText();
      }
    }

    f();

  },[alertSuccessOpen, _onHideSuccessText]);

  useEffect(()=>{
    if(!alertSuccessOpen) return;

    const timerId = setTimeout(()=>{
      setAlertSuccessOpen(false);
    },5000);
    return ()=>{clearTimeout(timerId)};
  },[alertSuccessOpen, _successText]);

  useEffect(()=>{
    if(!alertWarningOpen) return;

    const timerId = setTimeout(()=>{
      setAlertWarningOpen(false);
    },5000);
    return ()=>{clearTimeout(timerId)};
  },[alertWarningOpen, _warningText]);

  return (
    <div style={{marginTop:'5px', marginBottom:'5px'}}>
      <Collapse in={alertErrorOpen}>
        <Alert
          severity="error"
          action={
            <IconButton
              aria-label="close"
              color="inherit"
              size="small"
              onClick={() => {
                setAlertErrorOpen(false);
              }}
            >
              <CloseIcon fontSize="inherit" />
            </IconButton>
          }
        >
          {_errorText}
        </Alert>
      </Collapse>
      <Collapse in={alertWarningOpen}>
        <Alert
          severity="warning"
          action={
            <IconButton
              aria-label="close"
              color="inherit"
              size="small"
              onClick={() => {
                setAlertWarningOpen(false);
              }}
            >
              <CloseIcon fontSize="inherit" />
            </IconButton>
          }
        >
          {_warningText}
        </Alert>
      </Collapse>
      <Collapse in={alertSuccessOpen}>
        <Alert
          severity="success"
          action={
            <IconButton
              aria-label="close"
              color="inherit"
              size="small"
              onClick={() => {
                setAlertSuccessOpen(false);
              }}
            >
              <CloseIcon fontSize="inherit" />
            </IconButton>
          }
        >
          {_successText}
        </Alert>
      </Collapse>
    </div>
  );
};

export default AlertArea;
