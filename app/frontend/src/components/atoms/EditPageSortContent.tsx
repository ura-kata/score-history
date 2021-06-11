import { makeStyles, Theme, createStyles, Button } from "@material-ui/core";
import AddIcon from "@material-ui/icons/Add";
import React, { useMemo } from "react";
import { privateScoreItemUrlGen } from "../../global";
import { ScorePage } from "../../ScoreClientV2";
import AddCircleOutlineIcon from "@material-ui/icons/AddCircleOutline";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    toolBar: {
      width: "100%",
      height: "50px",
      display: "flex",
      "& button": {
        margin: "4px",
      },
    },
    editPageContainer: {
      width: "100%",
      display: "flex",
      flexWrap: "wrap",
    },
    itemContainer: {
      width: "200px",
      height: "200px",
      display: "flex",
      alignItems: "inherit",
      justifyContent: "center",
      textAlign: "center",
    },
    itemDivider: { height: "200px", width: "20px" },
    itemButton: {
      height: "100%",
      width: "100%",
      padding: 0,
      "& span": {
        height: "100%",
        width: "100%",
      },
    },
    itemDividerButton: {
      height: "100%",
      width: "100%",
      minWidth: 0,
      padding: "6px",
      "& span": {
        height: "100%",
        width: "100%",
      },
    },
    itemImg: {
      height: "100%",
    },
  })
);

export interface EditPageSortContentProps {
  ownerId?: string;
  scoreId?: string;
  pages?: ScorePage[];
  onCompleted?: () => void;
  onCancel?: () => void;
  onUpload?: () => void;
}

export default function EditPageSortContent(props: EditPageSortContentProps) {
  const classes = useStyles();
  const _ownerId = props.ownerId;
  const _scoreId = props.scoreId;
  const _pages = props.pages;

  const _onCompleted = props.onCompleted;
  const _onCancel = props.onCancel;
  const _onUpload = props.onUpload;

  const imageSrcList = useMemo(() => {
    if (!_ownerId) return [];
    if (!_scoreId) return [];
    if (!_pages) return [];
    return _pages.map((p, index) => {
      const thumbnailSrc = privateScoreItemUrlGen.getThumbnailImageUrl(
        _ownerId,
        _scoreId,
        p
      );
      return {
        id: p.id,
        thumbnailSrc: thumbnailSrc,
      };
    });
  }, [_ownerId, _scoreId, _pages]);

  const handleOnApplyClick = () => {
    if (_onCompleted) {
      _onCompleted();
    }
  };
  const handleOnCancelClick = () => {
    if (_onCancel) {
      _onCancel();
    }
  };
  const handleOnUploadClick = () => {
    if (_onUpload) {
      _onUpload();
    }
  };

  return (
    <div style={{ width: "100%" }}>
      <div className={classes.toolBar}>
        <Button variant="contained" onClick={handleOnApplyClick}>
          保存
        </Button>
        <Button variant="contained" onClick={handleOnCancelClick}>
          キャンセル
        </Button>
        <Button variant="contained" onClick={handleOnUploadClick}>
          画像をアップロードする
        </Button>
      </div>
      <div className={classes.editPageContainer}>
        {imageSrcList.map((x, index) => {
          return (
            <div key={x.id} style={{ display: "flex" }}>
              <div className={classes.itemContainer}>
                <img src={x.thumbnailSrc} className={classes.itemImg} />
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
