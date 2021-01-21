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
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@material-ui/core'
import PracticeManagerApiClient, { Score, ScoreVersion, ScoreVersionPage } from '../../PracticeManagerApiClient'
import ScoreDialog from '../molecules/ScoreDialog';
import { Link, useHistory, useRouteMatch } from "react-router-dom";
import { Skeleton, Timeline, TimelineConnector, TimelineContent, TimelineDot, TimelineItem, TimelineOppositeContent, TimelineSeparator } from '@material-ui/lab';

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
      if(!_socre) return;
    };
    f();
  },[_socre]);

  const timelineItems = !_socre ? [] : [..._socre.versions].reverse().map((version, index)=>{

    return (
      <TimelineItem>
        <TimelineSeparator>
          <TimelineDot>
            {/* Todo チェックしたかどうかをアイコンで表示する */}
          </TimelineDot>
          {index !== (_socre.versions.length - 1) ? (<TimelineConnector />) : (<></>)}
        </TimelineSeparator>
        <TimelineContent>
          <Button component={Link} to={`/home/${_socre?.name}/${version.version}`}>{version.version}</Button>
        </TimelineContent>
      </TimelineItem>
    );
  });

  return (
    <>
      <Grid container>
        <Grid item xs>
          <Typography variant="h4">{_socre?.title}</Typography>
        </Grid>
      </Grid>

      <Divider/>

      <Grid container>
        <Grid xs={4} container justify="center">
          <Typography variant="h5">バージョン</Typography>
          {/* Todo バージョンは長くなることが良そうされるのでスクロールできるようにする */}
          <Timeline align="left">
            {timelineItems}
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

interface PageDialogProps{
  page?: ScoreVersionPage;
  open: boolean;
  onClose?: ()=>void;
}
const PageDialog = (props: PageDialogProps) =>{
  const _page = props.page;
  const _open = props.open;
  const _onClose = props.onClose;

  return (
  <Dialog onClose={_onClose} open={_open}>
    <DialogTitle>
      <Typography align="center">{_page?.no}</Typography>
    </DialogTitle>
    <DialogContent dividers>
      <img src={_page?.image_url} alt={_page?.no.toString()} style={{height: "70vh"}}/>
    </DialogContent>
    <DialogActions>
      <Button autoFocus onClick={_onClose} color="primary">
        Close
      </Button>
    </DialogActions>
  </Dialog>
  );
};

// ------------------------------------------------------------------------------------------

interface ScoreVersionDetailContentProps{
  score?: Score;
  version?: number;
  pageNo?: number;
}

const ScoreVersionDetailContent = (props: ScoreVersionDetailContentProps) => {
  const _socre = props.score;
  const _version = props.version;
  const _pageNo = props.pageNo;

  const [scoreVersion, setScoreVersion] = useState<ScoreVersion | undefined>(undefined);
  const history = useHistory();

  let scoreVersionPage: ScoreVersionPage | undefined = undefined;

  useEffect(()=>{
    if(!_socre) return;
    if(_version === undefined) return;

    const f = async ()=>{
      try{
        const sv = await client.getScoreVersion(_socre.name, _version);
        setScoreVersion(sv);
      }
      catch(err){
        console.log(err);
      }
    };
    f();
  },[_socre, _version]);

  if(scoreVersion){
    scoreVersionPage = scoreVersion.pages.find((page)=>page.no === _pageNo);
  }

  const thumbnailContents = !scoreVersion ? [] : scoreVersion.pages.map((page, index)=>{
    return (
      <Grid item>
        <Button component={Link} to={`/home/${_socre?.name}/${_version}/${page.no}`}>
          <Paper>
            <Grid container>
              <Grid direction="row" xs={12} justify="center">
                <img src={page.thumbnail_url ?? page.image_url} height={"200px"} alt={page.no.toString()}/>
              </Grid>
              <Grid direction="row" xs={12}>
                <Typography align="center">{page.no}</Typography>
              </Grid>
            </Grid>
          </Paper>
        </Button>
      </Grid>
    );
  });

  const handleOnClose = ()=>{
    if(!_socre) return;
    history.push(`/home/${_socre.name}/${_version}/`);
  };

  return (
    <>
      <Grid container>
        <Grid item xs>
          <Typography variant="h4">{_socre?.title}</Typography>
        </Grid>
      </Grid>

      <Divider/>

      <Grid container>
        <Grid container xs={4}>
          <Grid direction="row" container>
            <Typography variant="h5">バージョン {_version}</Typography>
          </Grid>
          <Grid direction="row" container>
            <Typography variant="h5">説明</Typography>
            {scoreVersion?.description?.split('\n').map(t=>(<Typography>{t}</Typography>))}
          </Grid>
        </Grid>
        <Grid container xs alignItems="flex-start" justify="flex-start" alignContent="flex-start">
          {thumbnailContents}
        </Grid>
      </Grid>

      <PageDialog
        open={scoreVersionPage !== undefined}
        page={scoreVersionPage}
        onClose={handleOnClose}
      />
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

type HomeContentType = "home" | "detail" | "version" | "page";

const HomePage = () => {
  const classes = useStyles();

  const [scores, setScores] = useState<{[name: string]:Score}>({});
  const history = useHistory();
  const urlMatch = useRouteMatch<{
    scoreName?: string,
    version?: string,
    pageNo?: string}>("/home/:scoreName?/:version?/:pageNo?");

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

  let contentType: HomeContentType = "home";

  let score: undefined | Score = undefined;
  if(urlMatch){
    const scoreName = urlMatch.params.scoreName;
    if(scoreName){
      score = scores[scoreName];

      if(score){
        contentType = "detail";
      }
    }
  }

  let version: undefined | number = undefined;
  if(urlMatch){
    const versinText = urlMatch.params.version;
    version = versinText !== undefined ? parseInt(versinText) : undefined;
    if(version !== undefined){
      contentType = "version";
    }
  }

  let pageNo: undefined | number = undefined;
  if(urlMatch){
    const pageNoText = urlMatch.params.pageNo;
    pageNo = pageNoText !== undefined ? parseInt(pageNoText) : undefined;
    if(pageNo){
      contentType = "page";
    }
  }

  const handleScoreOnClick = (key: string, socre: Score) => {
    history.push(`/home/${key}/`);
  };
  const content = ((type: HomeContentType)=>{
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
      case "version":{
        return (
          <ScoreVersionDetailContent
            score={score}
            version={version}
          />
        );
      }
      case "page":{
        return (
          <ScoreVersionDetailContent
            score={score}
            version={version}
            pageNo={pageNo}
          />
        );
      }
      default: return (<></>)
  }})(contentType);

  const breadcrumbList = [(<Button component={Link} to="/">Home</Button>)];
  if(score){
    breadcrumbList.push((<Button component={Link} to={`/home/${score.name}/`}>{score.name}</Button>));

    if(version){
      breadcrumbList.push((<Button component={Link} to={`/home/${score.name}/${version}`}>version {version}</Button>));
    }
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
