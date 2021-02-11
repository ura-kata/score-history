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
import ScorePageDetailDialog from "../molecules/ScorePageDetailDialog";
import ScoreDetailContent from "../organisms/ScoreDetailContent";
import ScoreVersionDetailContent from "../organisms/ScoreVersionDetailContent";
import SocreListContent from "../organisms/SocreListContent";

const client = new PracticeManagerApiClient(
  process.env.REACT_APP_API_URI_BASE as string
);

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

  const content = ((type: HomeContentType) => {
    switch (type) {
      case "home": {
        return <SocreListContent scores={scores} />;
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
