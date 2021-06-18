import React, { Suspense, useEffect } from "react";
import { BrowserRouter as Router, Route, Switch } from "react-router-dom";

import useAppReducer, { AppContextDispatch, AppContext } from "./AppContext";
import { accessClient, apiClient, userClient } from "./global";

const MainPage = React.lazy(async () => {
  try {
    const mainPage = await import("./components/pages/MainPage");
    const userData = await userClient.getMyData();
    console.log("get user me");
    return { default: () => mainPage.default({ userData: userData }) };
  } catch (err) {
    console.log(err);
    accessClient.gotoSignInPage("");
    throw new Error();
  }
});

const App = () => {
  const [state, dispatch] = useAppReducer();

  useEffect(() => {
    const f = async () => {
      try {
        var userData = await userClient.getMyData();
        console.log("get user me");
        dispatch({ type: "updateUserData", payload: userData });
      } catch (err) {
        console.log(err);
        accessClient.gotoSignInPage("");
      }
    };

    f();
  }, []);

  return (
    <AppContext.Provider value={state}>
      <AppContextDispatch.Provider value={dispatch}>
        <Router>
          <Suspense fallback={<div>Loading...</div>}>
            <Switch>
              <Route path="/" component={MainPage} />
              {/* <Route path={["/", "/home/"]} component={ScoreListPage} exact />
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
            <Route path="/api-test" component={ApiTestPage} /> */}
            </Switch>
          </Suspense>
        </Router>
      </AppContextDispatch.Provider>
    </AppContext.Provider>
  );
};

export default App;
