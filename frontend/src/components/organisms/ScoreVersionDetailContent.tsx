import { Button, Divider, Grid, Paper, Typography } from "@material-ui/core";
import React, { useMemo } from "react";
import { Link, useHistory } from "react-router-dom";
import {
  ScoreV2PageObject,
  ScoreV2VersionObject,
} from "../../PracticeManagerApiClient";
import { ScorePage, ScoreProperty } from "../../ScoreClient";
import ScorePageDetailDialog from "../molecules/ScorePageDetailDialog";

export interface ScoreVersionDetailContentProps {
  owner: string;
  scoreName: string;
  property: ScoreProperty;
  version: string;
  pageIndex?: number;
  pages?: ScorePage[];
}

const ScoreVersionDetailContent = (props: ScoreVersionDetailContentProps) => {
  const _owner = props.owner;
  const _scoreName = props.scoreName;
  const _property = props.property;
  const _version = props.version;
  const _pageIndex = props.pageIndex;
  const _pages = props.pages;

  const history = useHistory();

  const actionsAndPageList = useMemo(() => {
    const ret = [] as {
      page: ScorePage;
      onPrev?: () => void;
      onNext?: () => void;
    }[];

    _pages?.forEach((page, index) => {
      const prevUrl =
        index === 0 ||
        _owner === undefined ||
        _scoreName === undefined ||
        _version === undefined
          ? undefined
          : `/home/${_owner}/${_scoreName}/version/${_version}/${index - 1}/`;

      const nextUrl =
        index === _pages.length - 1 ||
        _owner === undefined ||
        _scoreName === undefined ||
        _version === undefined
          ? undefined
          : `/home/${_owner}/${_scoreName}/version/${_version}/${index + 1}/`;
      ret.push({
        page: page,
        onPrev: prevUrl ? () => history.push(prevUrl) : undefined,
        onNext: nextUrl ? () => history.push(nextUrl) : undefined,
      });
    });

    return ret;
  }, [_owner, _scoreName, _version, history, _pages]);

  const actionsAndPage =
    _pageIndex === undefined ? undefined : actionsAndPageList[_pageIndex];

  const thumbnailContents = !_pages
    ? []
    : _pages.map((page, index) => {
        return (
          <Grid item key={page.number}>
            <Button
              component={Link}
              to={`/home/${_owner}/${_scoreName}/version/${_version}/${index}`}
            >
              <Paper>
                <Grid container justify="center">
                  <Grid item xs={12} style={{ textAlign: "center" }}>
                    <img
                      src={page.thumbnail ?? page.image}
                      height={"200px"}
                      alt={page.number}
                      style={{ userSelect: "none" }}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Typography align="center">p. {page.number}</Typography>
                  </Grid>
                </Grid>
              </Paper>
            </Button>
          </Grid>
        );
      });

  const handleOnClose = () => {
    if (!_owner) return;
    if (!_scoreName) return;
    history.push(`/home/${_owner}/${_scoreName}/version/${_version}/`);
  };

  return (
    <>
      <Grid container>
        <Grid item xs>
          <Typography variant="h4">{_property.title}</Typography>
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
              <Typography variant="h5">コメント</Typography>
            </Grid>
            <Grid item xs={12}></Grid>
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
