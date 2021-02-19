import React, { useCallback } from "react";
import GenericTemplate from "../templates/GenericTemplate";
import {
  createStyles,
  Button,
  makeStyles,
  Theme,
  TextField,
  Grid,
  Backdrop,
  CircularProgress,
  IconButton,
} from "@material-ui/core";
import { scoreClient } from "../../global";
import { NewScoreData } from "../../ScoreClient";
import { AppContext } from "../../AppContext";
import { useHistory } from "react-router-dom";
import { Alert, AlertTitle } from "@material-ui/lab";
import CloseIcon from "@material-ui/icons/Close";

//------------------------------------------------------------------------------

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    gredRoot: {
      width: "90%",
    },
    textField: {
      width: "100%",
    },
    backdrop: {
      zIndex: theme.zIndex.drawer + 1,
      color: "#fff",
    },
  })
);

const NewScorePage = () => {
  const classes = useStyles();

  const [scoreName, setScoreName] = React.useState("");
  const [scoreTitle, setScoreTitle] = React.useState("");
  const [scoreDescription, setScoreDescription] = React.useState("");
  const [backdropOpen, setBackdropOpen] = React.useState(false);
  const [errorMessage, setErrorMessage] = React.useState<string | undefined>();

  const appContext = React.useContext(AppContext);
  const _userMe = appContext.userMe;

  const history = useHistory();

  const handlerCreate = useCallback(async () => {
    if (!scoreName) {
      setErrorMessage("スコアの識別名を入力してください");
      return;
    }

    try {
      if (!_userMe?.name) {
        console.log("ログインしてください");
        //window.location.href = loginUrl;
        return;
      }
      const owner = _userMe.name;
      const newScoreData: NewScoreData = {
        owner: owner,
        scoreName: scoreName,
        property: {
          title: scoreTitle,
          description: scoreDescription,
        },
      };

      console.log(newScoreData);
      setBackdropOpen(true);

      await scoreClient.createScore(newScoreData);

      // Backdrop のテスト
      // await sleep(1000);

      history.replace(`/home/${owner}/${scoreName}`);
    } catch (err) {
      setErrorMessage("楽譜の作成に失敗しました! 別の識別名を入力してください");
    }
    setBackdropOpen(false);
  }, [_userMe, scoreName, scoreTitle, scoreDescription, history]);

  const handlerScoreName = useCallback(async (event) => {
    setScoreName(event.target.value);
  }, []);

  const handlerScoreTitle = useCallback(async (event) => {
    setScoreTitle(event.target.value);
  }, []);

  const handlerScoreDescription = useCallback(async (event) => {
    setScoreDescription(event.target.value);
  }, []);

  return (
    <GenericTemplate title="スコア">
      <div>
        <Grid container spacing={3} className={classes.gredRoot}>
          <Grid item xs={12}>
            <TextField
              label="スコアの識別名"
              className={classes.textField}
              value={scoreName}
              onChange={handlerScoreName}
            />
          </Grid>
          <Grid item xs={12}>
            <TextField
              label="スコアのタイトル (option)"
              className={classes.textField}
              value={scoreTitle}
              onChange={handlerScoreTitle}
            />
          </Grid>
          <Grid item xs={12}>
            <TextField
              label="スコアの説明 (option)"
              className={classes.textField}
              multiline
              value={scoreDescription}
              onChange={handlerScoreDescription}
            />
          </Grid>
          <Grid item xs={12}>
            {!errorMessage ? (
              <></>
            ) : (
              <Alert
                severity="error"
                action={
                  <IconButton
                    aria-label="close"
                    color="inherit"
                    size="small"
                    onClick={() => {
                      setErrorMessage(undefined);
                    }}
                  >
                    <CloseIcon fontSize="inherit" />
                  </IconButton>
                }
              >
                <AlertTitle>作成に失敗</AlertTitle>
                {errorMessage}
              </Alert>
            )}
          </Grid>

          <Grid item xs={12}>
            <Button variant="outlined" color="primary" onClick={handlerCreate}>
              作成
            </Button>
          </Grid>
          <Grid item xs={12}>
            <Backdrop className={classes.backdrop} open={backdropOpen}>
              <CircularProgress color="inherit" />
            </Backdrop>
          </Grid>
        </Grid>
      </div>
    </GenericTemplate>
  );
};

export default NewScorePage;
