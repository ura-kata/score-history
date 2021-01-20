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
  Breadcrumbs,
  Paper,
} from '@material-ui/core'
import PracticeManagerApiClient, { Score } from '../../PracticeManagerApiClient'
import ScoreDialog from '../molecules/ScoreDialog';
import { Link, useHistory, useRouteMatch } from "react-router-dom";
import { Timeline, TimelineConnector, TimelineContent, TimelineDot, TimelineItem, TimelineOppositeContent, TimelineSeparator } from '@material-ui/lab';

const client = new PracticeManagerApiClient(process.env.REACT_APP_API_URI_BASE as string);


// ------------------------------------------------------------------------------------------
interface ScoreListViewProps{
  scores: {[name: string]: Score};
  onClick?: (key: string, score: Score) => void;
}

const ScoreListView = (props: ScoreListViewProps) => {
  const _scores = props.scores;
  const _onClick = props.onClick;

  const classes = makeStyles((theme: Theme) =>
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
      }
    }))();

  return (
    <Grid container className={classes.scoreCardContainer}>
        {
           Object.entries(_scores).map(([key, score], i)=>(
            <Card key={i.toString()} className={classes.scoreCard}>
              <CardActionArea onClick={()=>{if(_onClick) _onClick(key, score);}}>
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
  );
};

// ------------------------------------------------------------------------------------------

interface ScoreDetailContentProps{
  score?: Score;
}

const ScoreDetailContent = (props: ScoreDetailContentProps) => {
  const _socre = props.score;

  useEffect(()=>{
    const f = async ()=>{

    };
    f();
  },[_socre]);


  return (
    <>
      <Grid container>
        <Grid item xs>
          <Typography variant="h4">{_socre?.title}</Typography>
        </Grid>
      </Grid>

      <Divider/>

      <Grid container>
        <Grid xs={4} container justify="flex-start">
          <Timeline align="left">
            <TimelineItem>
              <TimelineSeparator>
                <TimelineDot>

                </TimelineDot>
                <TimelineConnector />
              </TimelineSeparator>
              <TimelineContent>
                aaaaa
              </TimelineContent>
            </TimelineItem>
            {/***********************************************/}
            <TimelineItem>
              <TimelineSeparator>
                <TimelineDot>

                </TimelineDot>
                <TimelineConnector />
              </TimelineSeparator>
              <TimelineContent>
                aaaaa
              </TimelineContent>
            </TimelineItem>
            {/***********************************************/}
            <TimelineItem>
              <TimelineSeparator>
                <TimelineDot>

                </TimelineDot>
              </TimelineSeparator>
              <TimelineContent>
                <Paper elevation={3} style={{width:"70px"}}>
                  aaaaa bbbbb
                </Paper>
              </TimelineContent>
            </TimelineItem>
            {/***********************************************/}
          </Timeline>

        </Grid>
        <Grid xs>
          <Typography variant="h5">説明</Typography>
          {_socre?.description?.split('\n').map(t=>(<Typography>{t}</Typography>))}
        </Grid>
      </Grid>

    </>
  );
}

// ------------------------------------------------------------------------------------------

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
    breadcrumbsLink:{
      textDecoration: "none"
    }
  })
);

const HomePage = () => {
  const classes = useStyles();

  const [scores, setScores] = useState<{[name: string]:Score}>({});
  const history = useHistory();
  const scoreNameMatch = useRouteMatch<{scoreName: string}>("/home/:scoreName");

  useEffect(()=>{
    const f = async ()=>{

      try{
        const response = await client.getScores();

        const s: {[name: string]: Score} = {};
        response.forEach(x=>s[x.name] = x);
        setScores(s);
      }
      catch(err){
        console.log(err);
        setScores({});
      }
    };

    f();

  },[]);

  let contentType: "home" | "detail" = "home";

  let score = undefined;
  if(scoreNameMatch){
    const scoreName = scoreNameMatch.params.scoreName;
    score = scores[scoreName];

    if(score){
      contentType = "detail";
    }
  }

  const handleScoreOnClick = (key: string, socre: Score) => {
    history.push(`/home/${key}/`);
  };
  const content = ((type: "home" | "detail")=>{
    switch(type){
      case "home": {
        return (
          <>
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

            <ScoreListView scores={scores} onClick={handleScoreOnClick}/>
          </>
        );
      }
      case "detail": {
        return (
          <ScoreDetailContent
            score={score}
          />
        );
      }
  }})(contentType);

  const breadcrumbList = [(<Button component={Link} to="/">Home</Button>)];
  if(score){
    breadcrumbList.push((<Button component={Link} to={`/home/${score.name}/`}>{score.name}</Button>));
  }

  return (
    <GenericTemplate>
      <Breadcrumbs>
        {breadcrumbList}
      </Breadcrumbs>

      {content}

    </GenericTemplate>
  );

}

export default HomePage;
