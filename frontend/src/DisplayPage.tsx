import React, {useCallback} from 'react';
import GenericTemplate from './GenericTemplate'
import { createStyles, FormControl, FormHelperText, Input, Button, InputLabel, makeStyles, Theme, colors, GridList, GridListTile, GridListTileBar } from '@material-ui/core'
import { useDropzone } from 'react-dropzone'
import { readBuilderProgram } from 'typescript';
import PracticeManagerApiClient from './PracticeManagerApiClient'

const client = new PracticeManagerApiClient("http://localhost:5000/");

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
    }
  })
);

const DisplayPage = () => {
  const classes = useStyles();
  const [imageUrlList, setImageUrlList] = React.useState([] as string[]);

  const handlerDownload = async ()=>{
    setImageUrlList([]);

    const scoreVersion = await client.getScoreVersion();

    const urlList = scoreVersion.pages.map(page=>page.url);

    setImageUrlList(urlList);
  };

  return (
    <GenericTemplate title="ディスプレイスコア">
      <div>
        <Button onClick={handlerDownload}>ディスプレイ</Button>
        <GridList className={classes.imageList}>
          {
            imageUrlList.map((url, i) => (
              <GridListTile key={'image' + i.toString()}>
                <img src={url} className={classes.img} alt={i.toString()}></img>
              </GridListTile>
            ))
          }
        </GridList>
      </div>
    </GenericTemplate>
  )
}

export default DisplayPage;
