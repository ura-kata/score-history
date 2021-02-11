import { Button, Divider, Grid, Paper, Typography } from "@material-ui/core";
import React, { useEffect, useMemo, useState } from "react";
import { Link, useHistory } from "react-router-dom";
import { apiClient } from "../../global";
import {
  Score,
  ScoreVersion,
  ScoreVersionPage,
} from "../../PracticeManagerApiClient";
import ScorePageDetailDialog from "../molecules/ScorePageDetailDialog";

export interface ScoreVersionDetailContentProps {
  score?: Score;
  version?: number;
  pageNo?: number;
}

const ScoreVersionDetailContent = (props: ScoreVersionDetailContentProps) => {
  const _socre = props.score;
  const _version = props.version;
  const _pageNo = props.pageNo;

  const [scoreVersion, setScoreVersion] = useState<ScoreVersion | undefined>(
    undefined
  );
  const history = useHistory();

  useEffect(() => {
    if (!_socre) return;
    if (_version === undefined) return;

    const f = async () => {
      try {
        const sv = await apiClient.getScoreVersion(_socre.name, _version);
        setScoreVersion(sv);
      } catch (err) {
        console.log(err);
      }
    };
    f();
  }, [_socre, _version]);

  const actionsAndPageSet = useMemo(() => {
    const ret = {} as {
      [no: number]: {
        page: ScoreVersionPage;
        onPrev?: () => void;
        onNext?: () => void;
      };
    };

    scoreVersion?.pages.forEach((page, index) => {
      const prevUrl =
        index === 0 || _socre === undefined || _version === undefined
          ? undefined
          : `/home/${_socre.name}/${_version}/${
              scoreVersion.pages[index - 1].no
            }/`;

      const nextUrl =
        index === scoreVersion.pages.length - 1 ||
        _socre === undefined ||
        _version === undefined
          ? undefined
          : `/home/${_socre.name}/${_version}/${
              scoreVersion.pages[index + 1].no
            }/`;
      ret[page.no] = {
        page: page,
        onPrev: prevUrl ? () => history.push(prevUrl) : undefined,
        onNext: nextUrl ? () => history.push(nextUrl) : undefined,
      };
    });

    return ret;
  }, [_socre, _version, history, scoreVersion]);

  const actionsAndPage =
    _pageNo === undefined ? undefined : actionsAndPageSet[_pageNo];

  const thumbnailContents = !scoreVersion
    ? []
    : scoreVersion.pages.map((page, index) => {
        return (
          <Grid item key={page.no}>
            <Button
              component={Link}
              to={`/home/${_socre?.name}/${_version}/${page.no}`}
            >
              <Paper>
                <Grid container justify="center">
                  <Grid item xs={12} style={{ textAlign: "center" }}>
                    <img
                      src={page.thumbnail_url ?? page.image_url}
                      height={"200px"}
                      alt={page.no.toString()}
                      style={{ userSelect: "none" }}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Typography align="center">p. {page.no}</Typography>
                  </Grid>
                </Grid>
              </Paper>
            </Button>
          </Grid>
        );
      });

  const handleOnClose = () => {
    if (!_socre) return;
    history.push(`/home/${_socre.name}/${_version}/`);
  };

  return (
    <>
      <Grid container>
        <Grid item xs>
          <Typography variant="h4">{_socre?.title}</Typography>
        </Grid>
      </Grid>

      <Divider />

      <Grid container spacing={3}>
        <Grid item xs={12}>
          <Typography variant="h5">バージョン {_version}</Typography>
        </Grid>
        <Grid item xs={4}>
          <Grid container spacing={3}></Grid>
          <Grid container spacing={3}>
            <Grid item xs={12}>
              <Typography variant="h5">説明</Typography>
            </Grid>
            <Grid item xs={12}>
              {scoreVersion?.description?.split("\n").map((t, index) => (
                <Typography key={index}>{t}</Typography>
              ))}
            </Grid>
          </Grid>
        </Grid>
        <Grid item xs={8}>
          <Grid
            container
            alignItems="flex-start"
            justify="flex-start"
            alignContent="flex-start"
            spacing={1}
          >
            {thumbnailContents}
          </Grid>
        </Grid>
      </Grid>

      <ScorePageDetailDialog
        open={actionsAndPage !== undefined}
        page={actionsAndPage?.page}
        onClose={handleOnClose}
        onPrev={actionsAndPage?.onPrev}
        onNext={actionsAndPage?.onNext}
      />
    </>
  );
};

export default ScoreVersionDetailContent;
