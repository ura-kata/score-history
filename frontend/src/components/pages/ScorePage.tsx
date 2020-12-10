import React, {useCallback} from 'react';
import GenericTemplate from '../templates/GenericTemplate'
import { createStyles, FormControl, FormHelperText, Input, Button, InputLabel, makeStyles, Theme, colors, GridList, GridListTile, GridListTileBar, TextField, Grid, Card, CardContent, Typography } from '@material-ui/core'
import PracticeManagerApiClient, {Score} from '../../PracticeManagerApiClient'

const client = new PracticeManagerApiClient("http://localhost:5000/");

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    img:{
      height: "50vh"
    },
    imageDropZoneRoot: {
      margin:"20px",
    },
    imageDropZone:{
      width: "100%",
      height: "50px",
      cursor: "pointer",
      backgroundColor: colors.grey[200],
      textAlign: "center",
      '&:hover': {
        backgroundColor: colors.yellow[100],
      }
    },
    imageList:{
      backgroundColor: colors.grey[100],
    },
    gredRoot:{
      width: "90%"
    },
    textField:{
      width: "100%"
    },
    scoreCard:{
      width: "300px",
      margin: theme.spacing(1)
    },
    scoreCardName:{
      color: colors.grey[400]
    }
  })
);

const ScorePage = () => {
  const classes = useStyles();

  const [scoreName, setScoreName] = React.useState("");
  const [scoreTitle, setScoreTitle] = React.useState("");
  const [scoreDescription, setScoreDescription] = React.useState("");
  const [scores, setScores] = React.useState([] as Score[]);


  const handlerCreate = useCallback(async ()=>{
    try{
      const newScore = {
        name: scoreName,
        title: scoreTitle,
        description: scoreDescription
      };
      await client.createScore(newScore);
      alert(`'${scoreName}' を作成しました`);

    } catch (err){
      alert(err);
    }
  }, [scoreName, scoreTitle, scoreDescription]);

  const handlerScoreName = useCallback(async (event)=>{
    setScoreName(event.target.value);
  }, []);

  const handlerScoreTitle = useCallback(async (event)=>{
    setScoreTitle(event.target.value);
  }, []);

  const handlerScoreDescription = useCallback(async (event)=>{
    setScoreDescription(event.target.value);
  }, []);

  return (
    <GenericTemplate title='スコア'>
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
              label="スコアのタイトル"
              className={classes.textField}
              value={scoreTitle}
              onChange={handlerScoreTitle}
            />
          </Grid>
          <Grid item xs={12}>
            <TextField
              label="スコアの説明"
              className={classes.textField}
              multiline
              value={scoreDescription}
              onChange={handlerScoreDescription}
            />
          </Grid>
          <Grid item xs={12}>
          <Button variant="outlined" color="primary" onClick={handlerCreate}>作成</Button>
          </Grid>
        </Grid>
      </div>
      <div>
        {
          [0, 1, 2].map((score, i)=>(
            <Card key={i.toString()} className={classes.scoreCard}>
              <CardContent>
                <Typography variant="h5">スコアのタイトル{score.toString()}</Typography>
                <Typography variant="caption" className={classes.scoreCardName}>scoreName{score.toString()}</Typography>
                <Typography variant="subtitle1" gutterBottom>{"スコアの説明。"}</Typography>
              </CardContent>
            </Card>
          ))
        }
      </div>

    </GenericTemplate>
  );

}

export default ScorePage;
