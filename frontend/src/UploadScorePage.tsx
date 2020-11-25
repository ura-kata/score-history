import React, {useCallback} from 'react';
import GenericTemplate from './GenericTemplate'
import { createStyles, FormControl, FormHelperText, Input, Button, InputLabel, makeStyles, Theme, colors, GridList, GridListTile, GridListTileBar } from '@material-ui/core'
import { useDropzone } from 'react-dropzone'
import { readBuilderProgram } from 'typescript';

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

interface FileData{
  fileUrl: string;
  file: File;
}

const UploadScorePage = () => {
  const classes = useStyles();
  const [fileDataList, setFileDataList] = React.useState([] as FileData[]);

  const onDrop = useCallback((acceptedFiles) => {
    setFileDataList([])
    const loadedFileDataList: FileData[] = [];
    console.log("onDrop");

    acceptedFiles.forEach((f: File) => {
      console.log(f);
      const reader = new FileReader();

      reader.onabort = () => console.log('ファイルの読み込み中断');
      reader.onerror = () => console.log('ファイルの読み込みエラー');
      reader.onload = (e) => {
        // 正常に読み込みが完了した

          loadedFileDataList.push({
            fileUrl: e.target?.result as string,
            file: f
          });
          setFileDataList([...loadedFileDataList]);

      };

      reader.readAsDataURL(f);
    })

  },[]);
  const {getRootProps, getInputProps, isDragActive} = useDropzone({onDrop});

  const handlerUpload = ()=>{
    alert("upload click!")
  };

  return (
    <GenericTemplate title="アップロードスコア">
      <div>
        <Button onClick={handlerUpload}>アップロード</Button>
        <div className={classes.imageDropZoneRoot}>
          <div {...getRootProps()}>
            <input {...getInputProps()} />
            <div className={classes.imageDropZone}>
              このエリアをクリックするするか画像ドロップしてアップロードしてください
            </div>
          </div>
        </div>
        <GridList className={classes.imageList}>
          {
            fileDataList.map((fd, i) => (
              <GridListTile key={'image' + i.toString()}>
                <img src={fd.fileUrl} className={classes.img} alt={fd.file.name}></img>
                <GridListTileBar title={fd.file.name}/>
              </GridListTile>
            ))
          }
        </GridList>
      </div>
    </GenericTemplate>
  )
}

export default UploadScorePage;
