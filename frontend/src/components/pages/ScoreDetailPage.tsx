import {
  Button,
  ButtonGroup,
  colors,
  createStyles,
  Divider,
  Grid,
  makeStyles,
  Paper,
  Theme,
  Typography,
} from "@material-ui/core";
import { Alert } from "@material-ui/lab";
import React, { useEffect, useState } from "react";
import { Link, useHistory } from "react-router-dom";
import { scoreClient } from "../../global";
import { useScorePathParameter } from "../../hooks/scores/useScorePathParameter";
import PathCreator from "../../PathCreator";
import { ScorePage, ScoreProperty } from "../../ScoreClient";
import ScorePageDetailDialog from "../molecules/ScorePageDetailDialog";
import HomeTemplate from "../templates/HomeTemplate";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    versionButtonContainer: {
      overflow: "auto",
      display: "flex",
      padding: "5px",
    },
    versionButtonRoot: {
      display: "flex",
      alignItems: "center",
    },
    versionButton: {
      borderRadius: "30px",
      width: "60px",
      height: "60px",
      fontSize: "1.4rem",
    },
    versionButtonJoin: {
      display: "inline-block",
      width: "50px",
      height: "2px",
      backgroundColor: colors.grey[400],
      borderColor: colors.grey[400],
      border: "1px solid",
    },
  })
);

export default function ScoreDetailPage() {
  const pathParam = useScorePathParameter();

  const _owner = pathParam.owner;
  const _scoreName = pathParam.scoreName;
  const _pathCreator = new PathCreator();
  const _selectedPageIndex = pathParam.pageIndex;

  const classes = useStyles();
  const history = useHistory();

  const [loadScoreDataError, setLoadScoreDataError] = useState<string>();
  const [loadPagesError, setLoadPagesError] = useState<string>();

  const [property, setProperty] = useState<ScoreProperty>({});
  const [pages, setPages] = useState<ScorePage[]>([]);
  const [loadedScoreData, setLoadedScoreData] = useState(false);

  const loadScoreData = async (owner: string, scoreName: string) => {
    try {
      const scoreData = await scoreClient.getScore(owner, scoreName);
      setProperty(scoreData.scoreSummary.property);
      setLoadScoreDataError(undefined);
      setLoadedScoreData(true);
    } catch (err) {
      setLoadScoreDataError(`楽譜の情報取得に失敗しました`);
      console.log(err);
    }
  };

  const loadPages = async (owner: string, scoreName: string) => {
    try {
      const pages = await scoreClient.getPages(owner, scoreName);
      setPages(pages);
      setLoadPagesError(undefined);
    } catch (err) {
      setLoadPagesError(`ページの情報取得に失敗しました`);
      console.log(err);
    }
  };

  useEffect(() => {
    const f = async () => {
      if (!_owner) return;
      if (!_scoreName) return;
      if (!loadedScoreData) {
        loadScoreData(_owner, _scoreName);
      }
      loadPages(_owner, _scoreName);
    };
    f();
  }, [_owner, _scoreName, loadedScoreData]);

  const handleOnEditProperty = () => {
    if (!_owner) return;
    if (!_scoreName) return;
    history.push(_pathCreator.getEditPropertyPath(_owner, _scoreName));
  };

  const handleOnInitializePages = () => {
    if (!_owner) return;
    if (!_scoreName) return;
    history.push(_pathCreator.getEditPagePath(_owner, _scoreName));
  };

  const InitialVersionButton = () => (
    <Grid container direction="column" alignItems="center">
      <Grid item>
        <Button variant="outlined" onClick={handleOnInitializePages}>
          新しくページを登録する
        </Button>
      </Grid>
    </Grid>
  );
  const UpdateVersionButton = () => (
    <Grid container direction="column" alignItems="center">
      <Grid item>
        <Button variant="outlined" onClick={handleOnInitializePages}>
          ページを更新する
        </Button>
      </Grid>
    </Grid>
  );

  // TODO 最新のバージョンを表示することにする
  // version が選択されていない場合は最新のバージョンを表示することにする

  const thumbnailContents =
    _owner && _scoreName ? (
      <Grid container>
        {pages.map((page, index) => {
          return (
            <Grid item key={index}>
              <Button
                component={Link}
                to={_pathCreator.getPagePath(_owner, _scoreName, index)}
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
                      <Typography align="center">{page.number}</Typography>
                    </Grid>
                  </Grid>
                </Paper>
              </Button>
            </Grid>
          );
        })}
      </Grid>
    ) : (
      <></>
    );

  const selectedPage =
    _selectedPageIndex !== undefined ? pages[_selectedPageIndex] : undefined;

  const handleOnPageClose = () => {
    if (!_owner) return;
    if (!_scoreName) return;
    history.push(_pathCreator.getDetailPath(_owner, _scoreName));
  };
  const handleOnPagePrev =
    _owner &&
    _scoreName &&
    _selectedPageIndex !== undefined &&
    0 < _selectedPageIndex
      ? () => {
          history.push(
            _pathCreator.getPagePath(_owner, _scoreName, _selectedPageIndex - 1)
          );
        }
      : undefined;
  const handleOnPageNext =
    _owner &&
    _scoreName &&
    _selectedPageIndex !== undefined &&
    _selectedPageIndex < pages.length - 1
      ? () => {
          history.push(
            _pathCreator.getPagePath(_owner, _scoreName, _selectedPageIndex + 1)
          );
        }
      : undefined;

  return (
    <HomeTemplate>
      <Grid container spacing={2}>
        <Grid item xs={12}>
          {loadScoreDataError ? (
            <Alert severity="error">{loadScoreDataError}</Alert>
          ) : (
            <></>
          )}
        </Grid>
        <Grid item xs={12}>
          {loadPagesError ? (
            <Alert severity="error">{loadPagesError}</Alert>
          ) : (
            <></>
          )}
        </Grid>
        <Grid item xs={12}>
          <Grid container alignItems="center">
            <Grid item xs>
              <Typography variant="h4">
                {property.title ?? _scoreName}
              </Typography>
            </Grid>
            <Grid item xs>
              <Grid
                container
                alignItems="center"
                justify="flex-end"
                spacing={1}
              >
                <Grid item>
                  <ButtonGroup color="primary">
                    <Button onClick={handleOnEditProperty}>編集</Button>
                  </ButtonGroup>
                </Grid>
              </Grid>
            </Grid>
          </Grid>
        </Grid>
        <Grid item xs={12}>
          <Divider />
        </Grid>
        <Grid item xs={12}>
          <Grid container spacing={3}>
            <Grid item xs={12}>
              <Typography variant="h5">説明</Typography>
            </Grid>
            <Grid item xs={12}>
              {property.description?.split("\n").map((t, index) => (
                <Typography key={index}>{t}</Typography>
              ))}
            </Grid>
          </Grid>
        </Grid>
        <Grid item xs={12}>
          {0 < pages.length ? thumbnailContents : <InitialVersionButton />}
        </Grid>
        <Grid item xs={12}>
          {0 < pages.length ? <UpdateVersionButton /> : <></>}
        </Grid>
        <Grid item xs={12}>
          <ScorePageDetailDialog
            open={selectedPage !== undefined}
            page={selectedPage}
            onClose={handleOnPageClose}
            onPrev={handleOnPagePrev}
            onNext={handleOnPageNext}
          />
        </Grid>
      </Grid>
    </HomeTemplate>
  );
}
