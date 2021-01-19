import React, { useEffect, useState } from 'react';
import GenericTemplate from '../templates/GenericTemplate'
import {
  createStyles,
  makeStyles,
  Theme,
  colors,
  Grid,
  Card,
  CardContent,
  Typography,
  CardActionArea,
  Divider,
  ButtonGroup,
  Button,
} from '@material-ui/core'
import PracticeManagerApiClient, { Score } from '../../PracticeManagerApiClient'
import ScoreDialog from '../molecules/ScoreDialog';
import { Link } from "react-router-dom";

const client = new PracticeManagerApiClient(process.env.REACT_APP_API_URI_BASE as string);


const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    scoreCard:{
      width: "300px",
      margin: theme.spacing(1)
    },
    scoreCardName:{
      color: colors.grey[400]
    },
    scoreCardContainer:{
      margin: theme.spacing(3,0,0)
    },
  })
);

const HomePage = () => {
  const classes = useStyles();

  const [selectedScoreIndex, setSelectedScoreIndex] = useState<number | undefined>(undefined);
  const [dialogOpen, setDialogOpen] = useState(false);

  const [scores, setScores] = useState<Score[]>([]);

  useEffect(()=>{
    const f = async ()=>{

      try{
        const s = await client.getScores();
        setScores(s);
      }
      catch(err){
        console.log(err);
        setScores([]);
      }
    };

    f();

  },[]);

  return (
    <GenericTemplate>

      <Grid container>
        <Grid item xs>
          <Typography variant="h4">スコア一覧</Typography>
        </Grid>
        <Grid item xs>
          <ButtonGroup color="primary" style={{float: "right"}}>
            <Button component={Link} to="/new">新規</Button>
          </ButtonGroup>
        </Grid>

      </Grid>

      <Divider/>

      <Grid container className={classes.scoreCardContainer}>
        {
          scores.map((score, i)=>(
            <Card key={i.toString()} className={classes.scoreCard}>
              <CardActionArea onClick={()=>{setSelectedScoreIndex(i); setDialogOpen(true);}}>
                <CardContent>
                  <Typography variant="h5">{score.title}</Typography>
                  <Typography variant="caption" className={classes.scoreCardName}>{score.name}</Typography>
                  <Typography variant="subtitle1" gutterBottom>{score.description}</Typography>
                </CardContent>
              </CardActionArea>
            </Card>
          ))
        }
      </Grid>
      <ScoreDialog
        open={dialogOpen}
        score={ selectedScoreIndex === undefined ? undefined : scores[selectedScoreIndex]}
        onClose={()=>{setDialogOpen(false);}}
        onNext={()=>{
          if(selectedScoreIndex === undefined || !scores) return;

          setSelectedScoreIndex(Math.min(scores.length - 1, selectedScoreIndex + 1));
        }}
        onPrev={()=>{
          if(selectedScoreIndex === undefined || !scores) return;

          setSelectedScoreIndex(Math.max(0, selectedScoreIndex - 1));
        }}
      />

    </GenericTemplate>
  );

}

export default HomePage;
