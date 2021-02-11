import React, { useEffect, useReducer, useState } from "react";
import { BrowserRouter as Router, Route, Switch } from "react-router-dom";
import UploadScorePage from "./components/pages/UploadScorePage";
import DisplayPage from "./components/pages/DisplayPage";
import ApiTestPage from "./components/pages/ApiTestPage";
import ScorePage from "./components/pages/ScorePage";

import useAppReducer, { AppContextDispatch, AppContext } from "./AppContext";
import HomePage from "./components/pages/HomePage";
import { apiClient } from "./global";

const App = () => {
  const [state, dispatch] = useAppReducer();

  useEffect(() => {
    const f = async () => {
      try {
        const userMe = await apiClient.getUserMe();
        console.log("get user me");
        dispatch({ type: "updateUserMe", payload: userMe });
      } catch (err) {}
    };

    f();
  }, []);

  return (
    <AppContext.Provider value={state}>
      <AppContextDispatch.Provider value={dispatch}>
        <Router>
          <Switch>
            <Route path="/" component={HomePage} exact />
            <Route
              path="/home/:owner?/:scoreName?/:action?/:version?/:pageNo?"
              component={HomePage}
            />
            <Route path="/new" component={ScorePage} />
            <Route path="/upload" component={UploadScorePage} />
            <Route path="/display" component={DisplayPage} />
            <Route path="/api-test" component={ApiTestPage} />
          </Switch>
        </Router>
      </AppContextDispatch.Provider>
    </AppContext.Provider>
  );
};

export default App;
