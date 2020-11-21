import React, {useCallback} from 'react';
import GenericTemplate from './GenericTemplate'
import { createStyles, FormControl, FormHelperText, Input, Button, InputLabel, makeStyles, Theme, colors } from '@material-ui/core'
import { useDropzone } from 'react-dropzone'
import { readBuilderProgram } from 'typescript';

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    img:{
      height: "50vh"
    },
    imageDropZone:{
      width: "80%",
      height: "50px",
      cursor: "pointer",
      margin:"20px",
      backgroundColor: colors.grey[200],
      '&:hover': {
        backgroundColor: colors.yellow[100],
      }
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
        <div className={classes.imageDropZone}>
          <div {...getRootProps()}>
            <input {...getInputProps()} />
            <div>
              このエリアをクリックするするか画像ドロップしてアップロードしてください
            </div>
          </div>
        </div>
        <ul>
          {fileDataList.map((fd, i) => (
            <li>
              <img src={fd.fileUrl} key={'image' + i} className={classes.img}></img>
              <p>{fd.file.name}</p>
            </li>
          ))}
        </ul>
      </div>
    </GenericTemplate>
  )
}

export default UploadScorePage;
