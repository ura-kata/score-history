import {
  createStyles,
  IconButton,
  makeStyles,
  TextField,
  Theme,
} from "@material-ui/core";
import React, { useState } from "react";
import EditIcon from "@material-ui/icons/Edit";
import DoneIcon from "@material-ui/icons/Done";
import CloseIcon from "@material-ui/icons/Close";
import { scoreClientV2 } from "../../../global";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      width: "100%",
    },
    descriptionContainer: {
      width: "100%",
    },
    descriptionContainerNone: {
      display: "none",
    },
    description: {
      width: "auto",
    },
    editorContainer: {
      width: "100%",
    },
    editorContainerNone: { display: "none" },
    descP: {},
  })
);

export interface DetailEditableDescriptionProps {
  id?: string;
  description?: string;
  onChangeDescription?: (newDescription: string) => void;
}

export default function DetailEditableDescription(
  props: DetailEditableDescriptionProps
) {
  const _id = props.id;
  const _description = props.description;
  const _onChangeDescription = props.onChangeDescription;
  const classes = useStyles();

  const [edit, setEdit] = useState<Boolean>(false);
  const [newDescription, setNewDescription] = useState<string>("");
  const [updatedErrorText, setUpdatedErrorText] = useState<string>("");

  const handleOnChangeNewTitle = (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    setNewDescription(event.target.value);
  };

  const handleOnClickEdit = () => {
    if (!_id) return;
    setNewDescription(_description ?? "");
    setEdit(true);
    setUpdatedErrorText("");
  };

  const handleOnClickDone = async () => {
    if (!_id) return;

    if (newDescription === _description) {
      setUpdatedErrorText("変更されていません");
      return;
    }
    try {
      await scoreClientV2.updateDescription(_id, {
        description: newDescription,
      });
      setEdit(false);
      setUpdatedErrorText("");
      if (_onChangeDescription) {
        _onChangeDescription(newDescription);
      }
    } catch (err) {
      console.log(err);
      setUpdatedErrorText("楽譜の説明の更新に失敗");
    }
  };

  const handleOnClickCancel = () => {
    setEdit(false);
    setUpdatedErrorText("");
  };

  return (
    <div className={classes.root}>
      <div
        className={
          edit ? classes.descriptionContainerNone : classes.descriptionContainer
        }
      >
        <div className={classes.description}>
          {_description?.split("\n").map((p, index) => (
            <p key={index} className={classes.descP}>
              {p}
            </p>
          ))}
          <IconButton size="small" onClick={handleOnClickEdit}>
            <EditIcon />
          </IconButton>
        </div>
      </div>

      <div
        className={edit ? classes.editorContainer : classes.editorContainerNone}
      >
        <TextField
          variant="outlined"
          value={newDescription}
          onChange={handleOnChangeNewTitle}
          error={updatedErrorText ? true : false}
          helperText={updatedErrorText}
          multiline
        />
        <IconButton size="small" onClick={handleOnClickDone}>
          <DoneIcon />
        </IconButton>
        <IconButton size="small" onClick={handleOnClickCancel}>
          <CloseIcon />
        </IconButton>
      </div>
    </div>
  );
}
