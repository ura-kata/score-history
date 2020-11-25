import React from 'react';
import { Link, } from "react-router-dom";
import { Typography, } from '@material-ui/core';

const Copyright = () => {
  return (
    <Typography variant="body2" color="textSecondary" align="center">
      {"Copyright ©"}
      <Link color="inherit" to="/">
        uttne
      </Link>{" "}
      {new Date().getFullYear()}
      {"."}
    </Typography>
  );
};

export default Copyright;
