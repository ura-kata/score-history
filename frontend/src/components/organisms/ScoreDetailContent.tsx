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
import React from "react";
import { Link, useHistory } from "react-router-dom";
import { ScorePage, ScoreProperty } from "../../ScoreClient";
import ScorePageDetailDialog from "../molecules/ScorePageDetailDialog";
import { PathCreator } from "../pages/HomePage";

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

interface ScoreDetailContentProps {
  owner: string;
  scoreName: string;
  property: ScoreProperty;
  versions?: string[];
  selectedVersion?: string;
  pathCreator: PathCreator;
  pages?: ScorePage[];
  selectedPageIndex?: number;
}

const ScoreDetailContent = (props: ScoreDetailContentProps) => {
  const _owner = props.owner;
  const _scoreName = props.scoreName;
  const _property = props.property;
  const _pathCreator = props.pathCreator;
  const _pages = props.pages ?? [];
  const _selectedPageIndex = props.selectedPageIndex;

  const classes = useStyles();
  const history = useHistory();

  const _versions = props.versions ?? [];
  const _selectedVersion = props.selectedVersion;

  const versions = _versions.slice().reverse();

  const VersionTimeLine = () => (
    <Grid container direction="row" alignItems="center" justify="center">
      <Grid item xs={12}>
        <div className={classes.versionButtonContainer}>
          {versions.map((v, index) => {
            return (
              <div
                key={`${index}-version`}
                className={classes.versionButtonRoot}
              >
                <Button
                  onClick={() => {
                    const path = _pathCreator.getVersionPath(
                      _owner,
                      _scoreName,
                      v
                    );
                    history.push(path);
                  }}
                  variant={_selectedVersion === v ? "contained" : "outlined"}
                  className={classes.versionButton}
                >
                  {v}
                </Button>
                {index < versions.length - 1 ? (
                  <div className={classes.versionButtonJoin} />
                ) : (
                  <></>
                )}
              </div>
            );
          })}
        </div>
      </Grid>
    </Grid>
  );

  const handleOnEditProperty = () => {
    history.push(_pathCreator.getEditPropertyPath(_owner, _scoreName));
  };

  const handleOnInitializePages = () => {
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

  const thumbnailContents = _selectedVersion ? (
    <Grid container>
      {_pages.map((page, index) => {
        return (
          <Grid item key={index}>
            <Button
              component={Link}
              to={_pathCreator.getPagePath(
                _owner,
                _scoreName,
                _selectedVersion,
                index
              )}
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
      })}
    </Grid>
  ) : (
    <></>
  );

  const selectedPage =
    _selectedPageIndex !== undefined ? _pages[_selectedPageIndex] : undefined;

  const handleOnPageClose = () => {
    if (!_selectedVersion) return;
    history.push(
      _pathCreator.getVersionPath(_owner, _scoreName, _selectedVersion)
    );
  };
  const handleOnPagePrev =
    _selectedVersion &&
    _selectedPageIndex !== undefined &&
    0 < _selectedPageIndex
      ? () => {
          history.push(
            _pathCreator.getPagePath(
              _owner,
              _scoreName,
              _selectedVersion,
              _selectedPageIndex - 1
            )
          );
        }
      : undefined;
  const handleOnPageNext =
    _selectedVersion &&
    _selectedPageIndex !== undefined &&
    _selectedPageIndex < _pages.length - 1
      ? () => {
          history.push(
            _pathCreator.getPagePath(
              _owner,
              _scoreName,
              _selectedVersion,
              _selectedPageIndex + 1
            )
          );
        }
      : undefined;

  return (
    <Grid container spacing={2}>
      <Grid item xs={12}>
        <Grid container alignItems="center">
          <Grid item xs>
            <Typography variant="h4">
              {_property.title ?? _scoreName}
            </Typography>
          </Grid>
          <Grid item xs>
            <Grid container alignItems="center" justify="flex-end" spacing={1}>
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
            {_property.description?.split("\n").map((t, index) => (
              <Typography key={index}>{t}</Typography>
            ))}
          </Grid>
        </Grid>
      </Grid>
      <Grid item xs={12}>
        {0 < versions.length ? <VersionTimeLine /> : <InitialVersionButton />}
      </Grid>
      <Grid item xs={12}>
        {thumbnailContents}
      </Grid>
      <Grid item xs={12}>
        {0 < versions.length ? <UpdateVersionButton /> : <></>}
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
  );
};

export default ScoreDetailContent;
