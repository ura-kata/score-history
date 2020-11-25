import React from 'react';
import {  BrowserRouter as Router, Route, Switch } from "react-router-dom";
import UploadScorePage from './UploadScorePage';
import ApiTest from './ApiTest';


const App = () => {
  return (
    <Router>
      <Switch>
        <Route path="/" component={UploadScorePage} exact />
        <Route path="/api-test" component={ApiTest} exact />
      </Switch>

    </Router>
  );
}

export default App;
