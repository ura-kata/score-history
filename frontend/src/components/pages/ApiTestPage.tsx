import React, { useCallback, useState } from "react";
import GenericTemplate from "../templates/GenericTemplate";
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
  TextField,
} from "@material-ui/core";
import PracticeManagerApiClient, { NewScore, Score } from "../../PracticeManagerApiClient";

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

const ApiTestPage = () => {
  const classes = useStyles();
  const [version, setVersion] = React.useState('');

  // getVersion
  const handlerGetVersion = useCallback(async () => {
    try{
      const version = await apiClient.getVersion();
      setVersion(version);
      alert("Success");
    } catch(err) {
      alert(err);
    }

  },[]);

  const getVersionTag = (
    <Grid item xs={12}>
      <Paper className={classes.paper}>
        <Grid container spacing={2}>
          <Grid item xs={12}>
            <Typography>getVersion</Typography>
          </Grid>
          <Grid item xs={12}>
            <Button variant="outlined" color="primary" onClick={handlerGetVersion}>Exec</Button>
          </Grid>
          <Grid item xs={12}>
            <Typography>{version}</Typography>
          </Grid>
        </Grid>
      </Paper>
    </Grid>
  );


  // createScore
  const [createScore_param,setCreateScore_param] = useState<NewScore>({
    name: "",
    title: "",
    description: "",
  });
  const handlerCreateScore = useCallback(async () => {
    const newScore: NewScore = createScore_param;
    try{
      await apiClient.createScore(newScore);
      alert("Success");
    } catch(err){
      alert(err);
    }
  }, [createScore_param]);

  const handlerCreateScoreName = useCallback( (event) => {
    setCreateScore_param({
      name: event.target.id === 'createScore_param_name' ? event.target.value : createScore_param.name,
      title: event.target.id === 'createScore_param_title' ? event.target.value : createScore_param.title,
      description: event.target.id === 'createScore_param_description' ? event.target.value : createScore_param.description,
    });
  }, [createScore_param])

  const createScoreTag = (
    <Grid item xs={12}>
      <Paper className={classes.paper}>
        <Grid container spacing={2}>
          <Grid item xs={12}>
            <Typography>createScore</Typography>
          </Grid>
          <Grid item xs={12}>
            <TextField id={'createScore_param_name'} value={createScore_param.name} onChange={handlerCreateScoreName} label={'name'}></TextField>
          </Grid>
          <Grid item xs={12}>
            <TextField id={'createScore_param_title'} value={createScore_param.title} onChange={handlerCreateScoreName} label={'title'}></TextField>
          </Grid>
          <Grid item xs={12}>
            <TextField id={'createScore_param_description'} value={createScore_param.description} onChange={handlerCreateScoreName} label={'description'}></TextField>
          </Grid>
          <Grid item xs={12}>
            <Button variant="outlined" color="primary" onClick={handlerCreateScore}>Exec</Button>
          </Grid>
        </Grid>
      </Paper>
    </Grid>
  );


  // getScores
  const [getScores_scores,setGetScores_scores] = useState<Score[]>([]);
  const handlerGetScores = useCallback(async () => {
    try{
      const scores = await apiClient.getScores();
      setGetScores_scores(scores);
      alert("Success");
    } catch(err){
      alert(err);
    }
  }, []);

  const getScoresTag = (
    <Grid item xs={12}>
      <Paper className={classes.paper}>
        <Grid container spacing={2}>
          <Grid item xs={12}>
            <Typography>getScores</Typography>
          </Grid>
          <Grid item xs={12}>
            <Button variant="outlined" color="primary" onClick={handlerGetScores}>Exec</Button>
          </Grid>
          {
            getScores_scores.map((score, index)=>{
              return (
                <Grid item xs={12} key={index.toString()}>
                  <Typography>{JSON.stringify(score)}</Typography>
                </Grid>
              );
            })
          }
        </Grid>
      </Paper>
    </Grid>
  );

  return (
    <GenericTemplate title="アップロードスコア">
      <div>
        <Grid container spacing={3}>
          {getVersionTag}
          {createScoreTag}
          {getScoresTag}
        </Grid>
      </div>
    </GenericTemplate>
  );
};

export default ApiTestPage;
