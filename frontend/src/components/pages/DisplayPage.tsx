import React, {useCallback} from 'react';
import GenericTemplate from '../templates/GenericTemplate'
import { createStyles, FormControl, FormHelperText, Input, Button, InputLabel, makeStyles, Theme, colors, Grid, GridListTile, GridListTileBar } from '@material-ui/core'
import { useDropzone } from 'react-dropzone'
import { readBuilderProgram } from 'typescript';
import PracticeManagerApiClient from '../../PracticeManagerApiClient'
import ScoreViewer from '../atoms/ScoreViewer';

const client = new PracticeManagerApiClient(process.env.REACT_APP_API_URI_BASE as string);

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    img:{
      height: "50vh"
    },
    imageDropZoneRoot: {
      margin:"20px",
    },
    imageDropZone:{
      width: "100%",
      height: "50px",
      cursor: "pointer",
      backgroundColor: colors.grey[200],
      textAlign: "center",
      '&:hover': {
        backgroundColor: colors.yellow[100],
      }
    },
    imageList:{
      backgroundColor: colors.grey[100],
    },
    buttonGrid:{
      margin: theme.spacing(1,0,1)
    },
    socreGrid:{
      margin: theme.spacing(1,0,1)
    },
  })
);

const DisplayPage = () => {
  const classes = useStyles();
  const [imageUrlList, setImageUrlList] = React.useState([] as string[]);

  const handlerDownload = async ()=>{
    setImageUrlList([]);

    const scoreVersion = await client.getScoreVersion('test', 0);

    const urlList = scoreVersion.pages.map(page=>page.url);

    setImageUrlList(urlList);
  };

  return (
    <GenericTemplate title="スコアの表示">
      <Grid container>
        <Grid item xs={12} className={classes.buttonGrid}>
          <Button variant="outlined" color="primary" onClick={handlerDownload}>ダウンロード</Button>
        </Grid>
        <Grid item xs={12} className={classes.socreGrid}>
          <ScoreViewer imageUrls={imageUrlList}></ScoreViewer>
        </Grid>
      </Grid>
    </GenericTemplate>
  )
}

export default DisplayPage;
