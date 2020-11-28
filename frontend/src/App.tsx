import React from 'react';
import {  BrowserRouter as Router, Route, Switch } from "react-router-dom";
import UploadScorePage from './components/pages/UploadScorePage';
import DisplayPage from './components/pages/DisplayPage'
import ApiTestPage from './components/pages/ApiTestPage';


const App = () => {
  return (
    <Router>
      <Switch>
        <Route path="/" component={UploadScorePage} exact />
        <Route path="/display" component={DisplayPage} exact />
        <Route path="/api-test" component={ApiTestPage} exact />
      </Switch>

    </Router>
  );
}

export default App;
