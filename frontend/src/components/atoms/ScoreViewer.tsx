import React from 'react'
import { colors, createMuiTheme, createStyles, makeStyles, Theme, Grid, Paper, Slider, Mark, Button, IconButton } from '@material-ui/core';
import NavigateBeforeIcon from '@material-ui/icons/NavigateBefore';
import NavigateNextIcon from '@material-ui/icons/NavigateNext';

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    image:{
      height: '100%'
    },
    imagePaper:{
      height: '50vh',
      textAlign: 'center'
    },
    imageGrid:{
      padding: theme.spacing(2),
    },
    spaceGrid:{
      height: "30px",
    },
    sliderGrid:{
      padding: theme.spacing(1),
    }
  })
);

export interface ScoreViewrProps{
  imageUrls: string[]

}

const ScoreViewr = (props: ScoreViewrProps)=>{
  const [pageNo, setPageNo] = React.useState<number>(1);

  const classes = useStyles();

  let url: string = '';
  let marks: Mark[] = [];
  const length = props?.imageUrls?.length;
  if(length && 0 < length){
    url = props.imageUrls[0];
    marks = [
      {
        value: 1,
        label: '1'
      },
      {
        value: props.imageUrls.length,
        label: props.imageUrls.length.toString()
      }
    ]
  }

  const valueText = (value: number) => `${value}`;
  const handleChange = (event: any, newValue: number | number[]) => {
    setPageNo(newValue as number);
  }
  const getUrl = (no: number) => props.imageUrls[no - 1];

  const handleBefore = () => {
    if(1 < pageNo){
      setPageNo(pageNo - 1);
    }
  }
  const handleNext = () => {
    if(pageNo < length){
      setPageNo(pageNo + 1);
    }
  }

  return (
  <div>
    <Grid container spacing={3}>
      <Grid item xs={12} className={classes.imageGrid}>
        <Paper className={classes.imagePaper}>
          <img className={classes.image} src={getUrl(pageNo)} alt=''></img>
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
  </div>
  );

};

export default ScoreViewr;
