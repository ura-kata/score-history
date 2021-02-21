import React, { useEffect } from "react";
import { BrowserRouter as Router, Route, Switch } from "react-router-dom";
import UploadScorePage from "./components/pages/UploadScorePage";
import DisplayPage from "./components/pages/DisplayPage";
import ApiTestPage from "./components/pages/ApiTestPage";

import useAppReducer, { AppContextDispatch, AppContext } from "./AppContext";
import { apiClient } from "./global";
import NewScorePage from "./components/pages/NewScorePage";
import ScoreListPage from "./components/pages/ScoreListPage";
import ScoreDetailPage from "./components/pages/ScoreDetailPage";
import EditScorePropertyPage from "./components/pages/EditScorePropertyPage";
import UpdateScorePagePage from "./components/pages/UpdateScorePagePage";
import { HomeActionType } from "./PathCreator";

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
            <Route path={["/", "/home/"]} component={ScoreListPage} exact />
            <Route
              path={`/home/:owner?/:scoreName?/${((): HomeActionType =>
                "edit")()}/`}
              component={EditScorePropertyPage}
            />
            <Route
              path={`/home/:owner?/:scoreName?/${((): HomeActionType =>
                "edit-page")()}/`}
              component={UpdateScorePagePage}
            />
            <Route
              path={[
                "/home/:owner?/:scoreName?/",
                `/home/:owner?/:scoreName?/${((): HomeActionType =>
                  "page")()}/:pageIndex?/`,
              ]}
              component={ScoreDetailPage}
            />
            <Route path="/new" component={NewScorePage} />
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
