import MUIDatTable, { MUIDataTableColumnDef, MUIDataTableOptions } from "mui-datatables";

export interface ScoreTableData{
  name: string;
  title: string;
  lastVersion: number;
  description: string;
}

interface SocreTableRowDef{
  name: string;
  title: string;
  lastVersion: number;
  description: string;
}

export interface ScoreTableProps{
  title: string;
  data: ScoreTableData[];
  onSelectedChangeRow?: (scoreName: string)=>void;
}


const ScoreTable = (props: ScoreTableProps)=>{
  const _title = props.title;
  const _data = props.data;
  const _onSelectedChangeRow = props.onSelectedChangeRow;

  const columns = [{
    name: "name",
    label: "名前",
    options:{
      filter: true,
      sort: true,
    }
  },{
    name: "title",
    label: "タイトル",
    options:{
      filter: true,
      sort: true,
    }
  },{
    name: "lastVersion",
    label: "最新バージョン",
    options:{
      filter: true,
      sort: true,
    }
  },{
    name: "description",
    label: "説明",
    options:{
      filter: false,
      sort: false,
    }
  }] as MUIDataTableColumnDef[];

  const options = {
    resizableColumns: true,
    selectableRows: "single",
    selectableRowsHeader: true,
    onRowSelectionChange:(currentRowsSelected, allRowsSelected, rowsSelected)=>{
      if(!_onSelectedChangeRow) return;

      if(0 < allRowsSelected.length){
        _onSelectedChangeRow(d[allRowsSelected[0].dataIndex].name);
      }
      else{
        _onSelectedChangeRow("");
      }
    },
    selectableRowsOnClick: true,
    download: false,
    print: false,
    onRowsDelete:(rowsDeleted,data)=>false,
  } as MUIDataTableOptions;

  const d = _data.map(x=>({
    ...x,
  } as SocreTableRowDef) );

  return (<>
    <MUIDatTable
      title={_title}
      data={d}
      columns={columns}
      options={options}
    />

  </>);
}

export default ScoreTable;
