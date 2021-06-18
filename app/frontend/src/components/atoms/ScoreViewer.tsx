import React, { useCallback, useEffect } from "react";
import {
  createStyles,
  makeStyles,
  Theme,
  Grid,
  Paper,
  Slider,
  Mark,
  IconButton,
} from "@material-ui/core";
import NavigateBeforeIcon from "@material-ui/icons/NavigateBefore";
import NavigateNextIcon from "@material-ui/icons/NavigateNext";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      height: "100%",
    },
    image: {
      height: "100%",
    },
    imagePaper: {
      textAlign: "center",
      height: "100%",
    },
    imageGrid: {
      padding: theme.spacing(2),
      height: "100%",
    },
    spaceGrid: {
      height: "30px",
    },
    sliderGrid: {
      padding: theme.spacing(1),
      height: "auto",
    },
  })
);

export interface ScoreViewrProps {
  imageUrls: string[];
}

const ScoreViewr = (props: ScoreViewrProps) => {
  const [pageNo, setPageNo] = React.useState<number>(1);

  const classes = useStyles();

  const marks: Mark[] = [];
  const imageUrls = props.imageUrls;
  const length = imageUrls.length;

  if (0 < length) {
    marks.push({
      value: 1,
      label: "1",
    });
  }
  if (1 < length) {
    marks.push({
      value: length,
      label: length.toString(),
    });
  }

  const valueText = (value: number) => `${value}`;
  const handleChange = useCallback(
    (event: any, newValue: number | number[]) => {
      setPageNo(newValue as number);
    },
    []
  );

  const [imageUrl, setImageUrl] = React.useState("");

  const handleBefore = useCallback(() => {
    if (1 < pageNo) {
      setPageNo(pageNo - 1);
    }
  }, [pageNo]);
  const handleNext = useCallback(() => {
    if (pageNo < length) {
      setPageNo(pageNo + 1);
    }
  }, [pageNo, length]);

  useEffect(() => {
    const url = imageUrls[pageNo - 1];
    setImageUrl(url);
  }, [imageUrls, pageNo]);

  return (
    <>
      <Grid container spacing={1} className={classes.root}>
        <Grid item xs={12} className={classes.imageGrid}>
          <Paper className={classes.imagePaper}>
            <img className={classes.image} src={imageUrl} alt=""></img>
          </Paper>
        </Grid>
        <Grid item xs={12} className={classes.spaceGrid}></Grid>
        <Grid item xs={2} className={classes.sliderGrid}>
          <IconButton onClick={handleBefore}>
            <NavigateBeforeIcon />
          </IconButton>
        </Grid>
        <Grid item xs={8} className={classes.sliderGrid}>
          <Slider
            defaultValue={1}
            value={pageNo}
            getAriaValueText={valueText}
            aria-labelledby={"discrete-slider-always"}
            step={1}
            min={1}
            max={length}
            marks={marks}
            valueLabelDisplay="on"
            onChange={handleChange}
          />
        </Grid>
        <Grid item xs={2} className={classes.sliderGrid}>
          <IconButton onClick={handleNext}>
            <NavigateNextIcon />
          </IconButton>
        </Grid>
      </Grid>
    </>
  );
};

export default ScoreViewr;
