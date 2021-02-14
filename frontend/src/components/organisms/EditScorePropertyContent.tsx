import {
  Button,
  createStyles,
  Grid,
  makeStyles,
  TextField,
  Theme,
} from "@material-ui/core";
import { Alert } from "@material-ui/lab";
import React, { useState } from "react";
import { useHistory } from "react-router-dom";
import { scoreClient } from "../../global";
import { ScoreSummarySet } from "../../ScoreClient";
import { PathCreator } from "../pages/HomePage";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    textField: {
      width: "100%",
    },
  })
);

export interface EditScorePropertyContentProps {
  owner: string;
  scoreName: string;
  title?: string;
  description?: string;
  pathCreator: PathCreator;
  onLoadedScoreData?: (
    scoreSummarySet: ScoreSummarySet,
    versions: string[]
  ) => void;
}

const EditScorePropertyContent = (props: EditScorePropertyContentProps) => {
  const classes = useStyles();

  const _owner = props.owner;
  const _scoreName = props.scoreName;
  const _title = props.title;
  const _description = props.description;
  const _pathCreator = props.pathCreator;
  const _onLoadedScoreData = props.onLoadedScoreData;

  const [title, setTitle] = useState(_title);
  const [description, setDescription] = useState(_description);
  const [errorMessage, setErrorMessage] = useState<string>();

  const history = useHistory();

  const handleOnChangeTitle = (event: any) => {
    setTitle(event.target.value);
  };
  const handleOnChangeDescription = (event: any) => {
    setDescription(event.target.value);
  };

  const handleOnClickUpdate = async () => {
    const oldProperty = {
      title: props.title,
      description: props.description,
    };
    const newProperty = {
      title: title,
      description: description,
    };
    try {
      await scoreClient.updateProperty(
        _owner,
        _scoreName,
        oldProperty,
        newProperty
      );

      if (_onLoadedScoreData) {
        const scoreSet = await scoreClient.getScores();
        const versions = await scoreClient.getVersions(_owner, _scoreName);
        _onLoadedScoreData(scoreSet, versions);
      }
      setErrorMessage(undefined);

      history.replace(_pathCreator.getDetailPath(_owner, _scoreName));
    } catch (err) {
      console.log(err);
      setErrorMessage("変更に失敗しました");
    }
  };

  const disableUpdateButton =
    (_title ?? "") === (title ?? "") &&
    (_description ?? "") === (description ?? "");

  return (
    <Grid container spacing={3}>
      <Grid item xs={12}>
        <TextField
          label="楽譜のタイトル"
          className={classes.textField}
          value={title}
          onChange={handleOnChangeTitle}
        />
      </Grid>
      <Grid item xs={12}>
        <TextField
          label="楽譜の説明"
          multiline
          className={classes.textField}
          value={description}
          onChange={handleOnChangeDescription}
        />
      </Grid>
      <Grid item xs={12}>
        {errorMessage ? <Alert severity="error">{errorMessage}</Alert> : <></>}
      </Grid>
      <Grid item xs={12}>
        <Button
          variant="outlined"
          color="primary"
          onClick={handleOnClickUpdate}
          disabled={disableUpdateButton}
        >
          更新
        </Button>
      </Grid>
    </Grid>
  );
};

export default EditScorePropertyContent;
