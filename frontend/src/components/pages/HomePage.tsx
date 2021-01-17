import React, { useState } from 'react';
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
} from '@material-ui/core'
import PracticeManagerApiClient, { Score } from '../../PracticeManagerApiClient'
import ScoreDialog from '../molecules/ScoreDialog';

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

  const [selectedScore, setSelectedScore] = useState<Score | undefined>(undefined);
  const [dialogOpen, setDialogOpen] = useState(false);

  return (
    <GenericTemplate>

      <Typography variant="h4">スコア一覧</Typography>

      <Grid container className={classes.scoreCardContainer}>
        {
          [0, 1, 2, 3, 4, 5, 6, 7, 8, 9].map<Score>((i, _)=>({
            name: `scoreName${i}`,
            title: `スコアのタイトル${i}`,
            description: `スコアの説明。`,
            version_meta_urls: []
          })).map((score, i)=>(
            <Card key={i.toString()} className={classes.scoreCard}>
              <CardActionArea onClick={()=>{setSelectedScore(score); setDialogOpen(true);}}>
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
      <ScoreDialog open={dialogOpen} score={selectedScore} onClose={()=>{setDialogOpen(false);}}/>

    </GenericTemplate>
  );

}

export default HomePage;
