import React from 'react';
import {  BrowserRouter as Router, Route, Switch } from "react-router-dom";
import UploadScorePage from './UploadScorePage';


const App = () => {
  return (
    <Router>
      <Switch>
        <Route path="/" component={UploadScorePage} exact />
      </Switch>

    </Router>
  );
}

export default App;
