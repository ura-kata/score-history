import React, { useCallback } from "react";
import GenericTemplate from "./components/templates/GenericTemplate";
import {
  createStyles,
  Grid,
  Paper,
  FormControl,
  FormHelperText,
  Input,
  Button,
  InputLabel,
  makeStyles,
  Theme,
  colors,
  Typography,
} from "@material-ui/core";
import PracticeManagerApiClient from "./PracticeManagerApiClient";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    img: {
      height: "50vh",
    },
    imageDropZoneRoot: {
      margin: "20px",
    },
    paper: {
      padding: theme.spacing(2),
      margin: "auto",
      width: "90%",
    },
  })
);

const apiClient = new PracticeManagerApiClient("http://localhost:5000/");

const ApiTest = () => {
  const classes = useStyles();
  const [version, setVersion] = React.useState('');

  const handlerVersion = async () => {
    alert("click version");
    const version = await apiClient.getVersion();
    alert(version);
    setVersion(version)
  };

  return (
    <GenericTemplate title="アップロードスコア">
      <div>
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Paper className={classes.paper}>
              <Grid container spacing={2}>
                <Grid item>
                  <Button onClick={handlerVersion}>バージョン</Button>
                </Grid>
                <Grid item>
                  <Typography>{version}</Typography>
                </Grid>
              </Grid>
            </Paper>
          </Grid>
        </Grid>
      </div>
    </GenericTemplate>
  );
};

export default ApiTest;
