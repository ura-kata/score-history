import React from 'react';
import {  BrowserRouter as Router, Route, Switch } from "react-router-dom";
import UploadScorePage from './components/pages/UploadScorePage';
import DisplayPage from './components/pages/DisplayPage'
import ApiTestPage from './components/pages/ApiTestPage';
import ScorePage from './components/pages/ScorePage'


const App = () => {
  return (
    <Router>
      <Switch>
        <Route path="/" component={ScorePage} exact />
        <Route path="/upload" component={UploadScorePage} />
        <Route path="/display" component={DisplayPage} />
        <Route path="/api-test" component={ApiTestPage} />
      </Switch>

    </Router>
  );
}

export default App;
