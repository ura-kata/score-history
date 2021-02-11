import React, { useEffect, useMemo, useState } from "react";
import GenericTemplate from "../templates/GenericTemplate";
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
} from "@material-ui/core";
import PracticeManagerApiClient, {
  Score,
  ScoreVersion,
  ScoreVersionPage,
} from "../../PracticeManagerApiClient";
import { Link, useHistory, useRouteMatch } from "react-router-dom";
import {
  Timeline,
  TimelineConnector,
  TimelineContent,
  TimelineDot,
  TimelineItem,
  TimelineSeparator,
} from "@material-ui/lab";
import ScorePageDetailDialog from "../organisms/ScorePageDetailDialog";
import ScoreDetailContent from "../organisms/ScoreDetailContent";
import ScoreVersionDetailContent from "../organisms/ScoreVersionDetailContent";

const client = new PracticeManagerApiClient(
  process.env.REACT_APP_API_URI_BASE as string
);

// ------------------------------------------------------------------------------------------
interface ScoreListViewProps {
  scores: { [name: string]: Score };
  onClick?: (key: string, score: Score) => void;
}

const ScoreListView = (props: ScoreListViewProps) => {
  const _scores = props.scores;
  const _onClick = props.onClick;

  const classes = makeStyles((theme: Theme) =>
    createStyles({
      scoreCard: {
        width: "300px",
        margin: theme.spacing(1),
      },
      scoreCardName: {
        color: colors.grey[400],
      },
      scoreCardContainer: {
        margin: theme.spacing(3, 0, 0),
      },
    })
  )();

  return (
    <Grid container className={classes.scoreCardContainer}>
      {Object.entries(_scores).map(([key, score], i) => (
        <Card key={i.toString()} className={classes.scoreCard}>
          <CardActionArea
            onClick={() => {
              if (_onClick) _onClick(key, score);
            }}
          >
            <CardContent>
              <Typography variant="h5">{score.title}</Typography>
              <Typography variant="caption" className={classes.scoreCardName}>
                {score.name}
              </Typography>
              <Typography variant="subtitle1" gutterBottom>
                {score.description}
              </Typography>
            </CardContent>
          </CardActionArea>
        </Card>
      ))}
    </Grid>
  );
};

// ------------------------------------------------------------------------------------------

// ------------------------------------------------------------------------------------------

// ------------------------------------------------------------------------------------------

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    scoreCard: {
      width: "300px",
      margin: theme.spacing(1),
    },
    scoreCardName: {
      color: colors.grey[400],
    },
    scoreCardContainer: {
      margin: theme.spacing(3, 0, 0),
    },
    breadcrumbsLink: {
      textDecoration: "none",
    },
  })
);

type HomeContentType = "home" | "detail" | "version" | "page";

const HomePage = () => {
  const classes = useStyles();

  const [scores, setScores] = useState<{ [name: string]: Score }>({});
  const history = useHistory();
  const urlMatch = useRouteMatch<{
    scoreName?: string;
    version?: string;
    pageNo?: string;
  }>("/home/:scoreName?/:version?/:pageNo?");

  useEffect(() => {
    const f = async () => {
      try {
        const response = await client.getScores();

        const s: { [name: string]: Score } = {};
        response.forEach((x) => (s[x.name] = x));
        setScores(s);
      } catch (err) {
        console.log(err);
        setScores({});
      }
    };

    f();
  }, []);

  let contentType: HomeContentType = "home";

  let score: undefined | Score = undefined;
  if (urlMatch) {
    const scoreName = urlMatch.params.scoreName;
    if (scoreName) {
      score = scores[scoreName];

      if (score) {
        contentType = "detail";
      }
    }
  }

  let version: undefined | number = undefined;
  if (urlMatch) {
    const versinText = urlMatch.params.version;
    version = versinText !== undefined ? parseInt(versinText) : undefined;
    if (version !== undefined) {
      contentType = "version";
    }
  }

  let pageNo: undefined | number = undefined;
  if (urlMatch) {
    const pageNoText = urlMatch.params.pageNo;
    pageNo = pageNoText !== undefined ? parseInt(pageNoText) : undefined;
    if (pageNo) {
      contentType = "page";
    }
  }

  const handleScoreOnClick = (key: string, socre: Score) => {
    history.push(`/home/${key}/`);
  };
  const content = ((type: HomeContentType) => {
    switch (type) {
      case "home": {
        return (
          <>
            <Grid container>
              <Grid item xs>
                <Typography variant="h4">スコア一覧</Typography>
              </Grid>
              <Grid item xs>
                <ButtonGroup color="primary" style={{ float: "right" }}>
                  <Button component={Link} to="/new">
                    新規
                  </Button>
                </ButtonGroup>
              </Grid>
            </Grid>

            <Divider />

            <ScoreListView scores={scores} onClick={handleScoreOnClick} />
          </>
        );
      }
      case "detail": {
        return <ScoreDetailContent score={score} />;
      }
      case "version": {
        return <ScoreVersionDetailContent score={score} version={version} />;
      }
      case "page": {
        return (
          <ScoreVersionDetailContent
            score={score}
            version={version}
            pageNo={pageNo}
          />
        );
      }
      default:
        return <></>;
    }
  })(contentType);

  const breadcrumbList = [
    <Button key={0} component={Link} to="/">
      Home
    </Button>,
  ];
  if (score) {
    breadcrumbList.push(
      <Button
        key={breadcrumbList.length}
        component={Link}
        to={`/home/${score.name}/`}
      >
        {score.name}
      </Button>
    );

    if (version) {
      breadcrumbList.push(
        <Button
          key={breadcrumbList.length}
          component={Link}
          to={`/home/${score.name}/${version}`}
        >
          version {version}
        </Button>
      );
    }
  }

  return (
    <GenericTemplate>
      <Breadcrumbs>{breadcrumbList}</Breadcrumbs>

      {content}
    </GenericTemplate>
  );
};

export default HomePage;
