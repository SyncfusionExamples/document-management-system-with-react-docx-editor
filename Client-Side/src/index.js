import { createRoot } from 'react-dom/client';
import './index.css';
import * as React from 'react';
import { useEffect, useRef, useState } from 'react';
import { DialogComponent } from '@syncfusion/ej2-react-popups';
import { GridComponent, ColumnsDirective, ColumnDirective, Inject, CommandColumn, Page } from '@syncfusion/ej2-react-grids';
import { TreeViewComponent, ToolbarComponent, ItemsDirective, ItemDirective } from '@syncfusion/ej2-react-navigations';
import { DocumentEditorContainerComponent, Toolbar, CollaborativeEditingHandler, Ribbon } from '@syncfusion/ej2-react-documenteditor';
import { DocumentEditor } from '@syncfusion/ej2-react-documenteditor';
import { TitleBar } from './title-bar';
import { HubConnectionBuilder, HttpTransportType, HubConnectionState, HubConnection } from '@microsoft/signalr';

DocumentEditorContainerComponent.Inject(Toolbar,Ribbon);
//The current user for the demo. This user is saved with each document version
//and shown in the version history (can later come from login/session instead).
const predefinedUsers = ['Nancy Davolio'];
const DocumentList = () => {
  useEffect(() => {
    rendereComplete();
  }, []);
  let dialogInstance = useRef(null);
  let editorDialogInstance = useRef(null);
  const gridInstance = useRef(null);
  const [documents, setDocuments] = useState([]);
  const [versionHistoryData, setVersionHistoryData] = useState([]);
  const [selectedUser, setSelectedUser] = useState(predefinedUsers[0]);
  const [isDialogOpen, setDialogOpen] = useState(false);
  const [isEditorDialogOpen, setEditorDialogOpen] = useState(false);
  const [isGridOpen, setGridOpen] = useState(true);
  const [visible, setVisible] = useState(false);
  const [isNewDocDialogOpen, setNewDocDialogOpen] = useState(false);
  const [newDocName, setNewDocName] = useState('');
  const [name, setName] = useState('');
  const commands = [
    { type: 'View', buttonOption: { cssClass: "e-icons e-eye e-flat", title: "View latest version" } },
    { type: 'Edit', buttonOption: { cssClass: "e-icons e-edit e-flat", title: "Edit latest version" } },
    { type: 'View', buttonOption: { cssClass: "e-icons e-thumbnail e-flat", title: "Version history" } },
  ];
  let downloadButton;
  useEffect(() => {
    if (isGridOpen) {
      refreshDocumentList();
    }
  }, [isGridOpen]);
  let hostUrl = "https://services.syncfusion.com/react/production/api/documenteditor/";
  let container = useRef(null);
  let editorcontainer = useRef(null);
  let treeObj = useRef(null);
  let titleBar;
  let serviceUrl = 'http://localhost:62869/';
  let currentUser = 'Guest user';
  let operations = [];
  let contentChanged = false;
  let connection;
 
  let selectedNode;
  let url = 'http://localhost:62870/api/documenteditor/';
  //Fetches the list of available documents from the server. No static data is used.
  const refreshDocumentList = () => {
    let baseUrl = url + 'GetAllDocumentsFromS3';
    let httpRequest = new XMLHttpRequest();
    httpRequest.open('POST', baseUrl, true);
    httpRequest.setRequestHeader('Content-Type', 'application/json;charset=UTF-8');
    httpRequest.onreadystatechange = () => {
      if (httpRequest.readyState === 4 && (httpRequest.status === 200 || httpRequest.status === 304)) {
        let response = JSON.parse(httpRequest.responseText);
        setDocuments(response);
      }
    };
    httpRequest.send(JSON.stringify({}));
  };
  const newDocumentClick = () => {
    setNewDocName('');
    setNewDocDialogOpen(true);
    //Clear any previously typed name so the dialog opens with an empty input.
    const nameInput = document.getElementById('newDocumentName');
    if (nameInput) {
      nameInput.value = '';
    }
  };
  //Creates a new blank document on the client and asks the user for the document name.
  const createNewDocument = () => {
    //Read the name directly from the input element. The dialog buttons are rendered by the
    //Syncfusion dialog (not React) and may hold a stale closure of the state, so the DOM
    //value is the only reliable source of what the user typed.
    let documentName = '';
    const nameInput = document.getElementById('newDocumentName');
    if (nameInput) {
      documentName = nameInput.value;
    }
    //Fall back to the state if the element is not available.
    if (!documentName || documentName.trim() === '') {
      documentName = newDocName || '';
    }
    documentName = documentName.trim();
    if (documentName === '') {
      documentName = 'Untitled';
    }
    //Avoid invalid characters for folder name on the server.
    documentName = documentName.replace(/[\\/:*?"<>|]/g, '');
    setNewDocDialogOpen(false);
    setGridOpen(false);
    setDialogOpen(true);
    //Open the blank document first and then set the name, otherwise openBlank resets the name to 'Untitled'.
    container.current.documentEditor.openBlank();
    container.current.documentEditor.documentName = documentName;
    container.current.documentEditor.isReadOnly = false;
    container.current.documentEditor.enableContextMenu = true;
    //New documents are editable, so show the modern Ribbon interface.
    container.current.toolbarMode = 'Ribbon';
    container.current.documentEditor.resize();
    //The Ribbon renders asynchronously; resize again once it is in the DOM so the editor fills the dialog.
    setTimeout(() => {
      if (container.current) {
        container.current.documentEditor.resize();
      }
    }, 200);
    const downloadButton = document.getElementById("documenteditor-share");
    if (downloadButton) {
      downloadButton.style.display = "block";
    }
    const closeButton = document.getElementById("de-close");
    if (closeButton) {
      closeButton.style.display = "block";
    }
    document.getElementById("documenteditor_title_name").textContent = documentName;
  };
  const onLoadDefault = () => {
    titleBar.updateDocumentTitle();
    container.current.documentChange = () => {
      titleBar.updateDocumentTitle();
      container.current.documentEditor.focusIn();
    };
    container.current.documentEditorSettings.showRuler = true;
    //Set the predefined user chosen in the header as the editor's current user.
    container.current.currentUser = selectedUser;
  };
  const rendereComplete = () => {
    window.onbeforeunload = function () {
      return "Want to save your changes?";
    };
    container.current.documentEditor.pageOutline = "#E0E0E0";
    container.current.documentEditor.acceptTab = true;
    container.current.documentEditor.resize();
    titleBar = new TitleBar(document.getElementById("documenteditor_titlebar"), container.current.documentEditor, true, false, dialogInstance.current,true);
    //The Save button in the title bar triggers the save to the server.
    titleBar.saveHandler = saveDocument;
    onLoadDefault();
  };
  const saveDocument = (onSaved) => {
    //You can save the document as below
    container.current.documentEditor.saveAsBlob('Docx').then((blob) => {
      let fileReader = new FileReader();
      fileReader.onload = () => {
        let base64String = fileReader.result;
        //Use the document name; if it is empty fall back to the name shown in the title bar.
        let documentName = container.current.documentEditor.documentName;
        if (!documentName || documentName === '') {
          const titleElement = document.getElementById('documenteditor_title_name');
          if (titleElement && titleElement.textContent) {
            documentName = titleElement.textContent;
          }
        }
        if (!documentName || documentName === '') {
          documentName = 'Untitled';
        }
        let responseData = {
          fileName: documentName + '.docx',
          modifiedUser: selectedUser,
          documentData: base64String,
        };
        let baseUrl = url + 'AutoSaveToS3';
        let httpRequest = new XMLHttpRequest();
        httpRequest.open('POST', baseUrl, true);
        httpRequest.setRequestHeader(
          'Content-Type',
          'application/json;charset=UTF-8'
        );
        //Refresh the document list once the save completes on the server.
        httpRequest.onreadystatechange = () => {
          if (httpRequest.readyState === 4) {
            if (httpRequest.status === 200) {
              refreshDocumentList();
            }
            if (onSaved) {
              onSaved(httpRequest.status);
            }
          }
        };
        httpRequest.send(JSON.stringify(responseData));
      };
      fileReader.readAsDataURL(blob);
    });
  };

  const dialogClose = () => {
    //Saving is now explicit via the Save button in the title bar; nothing to do on close.
    setDialogOpen(false);
    setGridOpen(true);
  };
  const dialogOpen = () => {
    setDialogOpen(true);
    container.current.documentEditor.resize();
  };
  const editorDialogOpen = () => {
    setEditorDialogOpen(true);
    editorcontainer.current.documentEditor.resize();
  };

  const editorDialogClose = () => {
    setEditorDialogOpen(false);
  };
  const getVersionHistory = (name) => {
    let responseData = {
      fileName: name,
    };
    let baseUrl = url + 'GetVersionDataFromS3';
    let httpRequest = new XMLHttpRequest();
    httpRequest.open('POST', baseUrl, true);
    httpRequest.setRequestHeader(
      'Content-Type',
      'application/json;charset=UTF-8'
    );
    httpRequest.onreadystatechange = () => {
      if (httpRequest.readyState === 4) {
        if (httpRequest.status === 200 || httpRequest.status === 304) {
          let response = JSON.parse(httpRequest.responseText);
          editorcontainer.current.documentEditor.open((response.Document));
          //Bind the version history returned by the server to the tree view. No static data.
          setVersionHistoryData(response.Data);
          treeObj.current.fields = {
            dataSource: response.Data,
            id: 'id',
            text: 'name',
            child: 'subChild',
          };
          
        } 
      }
    };
    httpRequest.send(JSON.stringify(responseData));
  };

  const loadLatestVersion = (roomName) => {
    let responseData;
    responseData = {
      fileName: roomName,
    };
    let baseUrl = url + 'LoadLatestVersionDocumentFromS3';
    let httpRequest = new XMLHttpRequest();
    httpRequest.open('POST', baseUrl, true);
    httpRequest.setRequestHeader(
      'Content-Type',
      'application/json;charset=UTF-8'
    );
    httpRequest.onreadystatechange = () => {
      if (httpRequest.readyState === 4) {
        if (httpRequest.status === 200 || httpRequest.status === 304) {
          container.current.documentEditor.open(httpRequest.responseText);
          container.current.documentEditor.resize();
        }
      }
    };
    httpRequest.send(JSON.stringify(responseData));
  };
  const onCommandClicked = (args) => {
    const cssClass = args.target.className;
    const currentDocument = args.rowData.FileName;
    container.current.documentEditor.documentName = currentDocument.replace('.docx', '');
    editorcontainer.current.documentEditor.documentName = currentDocument.replace('.docx', '');
    if (cssClass.includes('e-icons e-eye e-flat')) {
      setDialogOpen(true);
      setGridOpen(false);
      //Load the latest version of the document from the server.
      loadLatestVersion(currentDocument);
      container.current.documentEditor.isReadOnly = true;
      container.current.documentEditor.enableContextMenu = false;
      //View mode uses the compact classic toolbar with read-only friendly commands.
      container.current.toolbarMode = 'Toolbar';
      container.current.documentEditor.resize();
      const downloadButton = document.getElementById("documenteditor-share");
      if (downloadButton) {
        downloadButton.style.display = "none";
      }
      const closeButton = document.getElementById("de-close");
      if (closeButton) {
        closeButton.style.display = "block";
      }
      container.current.documentEditor.documentName = args.rowData.FileName.replace(".docx", "");
      document.getElementById("documenteditor_title_name").textContent = container.current.documentEditor.documentName;
      container.current.toolbarItems = ['Open', 'Separator', 'Find'];
      //View-only mode: hide the Save button on the title bar since editing isn't allowed.
      if (titleBar) {
        titleBar.setSaveVisible(false);
      }
    }
    else if (cssClass.includes('e-icons e-edit e-flat')) {
      setDialogOpen(true);
      setGridOpen(false);
      //Load the latest version of the document from the server.
      loadLatestVersion(currentDocument);
      container.current.documentEditor.isReadOnly = false;
      container.current.documentEditor.enableContextMenu = true;
      //Show the modern Ribbon interface (Word-like tabs) in edit mode with the full set of editing commands.
      //Note: toolbarItems only applies to the classic Toolbar mode, so it is not used here.
      container.current.toolbarMode = 'Ribbon';
      container.current.documentEditor.resize();
      //The Ribbon renders asynchronously; resize again once it is in the DOM so the editor fills the dialog.
      setTimeout(() => {
        if (container.current) {
          container.current.documentEditor.resize();
        }
      }, 200);
      const downloadButton = document.getElementById("documenteditor-share");
      if (downloadButton) {
        downloadButton.style.display = "block";
      }
      const closeButton = document.getElementById("de-close");
      if (closeButton) {
        closeButton.style.display = "block";
      }
      container.current.documentEditor.documentName = args.rowData.FileName.replace(".docx", "");
      document.getElementById("documenteditor_title_name").textContent = container.current.documentEditor.documentName;
      //Edit mode: make the Save button visible again so users can persist edits as a new version.
      if (titleBar) {
        titleBar.setSaveVisible(true);
      }
    } else if (cssClass.includes('e-icons e-thumbnail e-flat')) {
      getVersionHistory(args.rowData.FileName);
      editorcontainer.current.documentEditor.enableContextMenu = false;
      editorcontainer.current.documentEditor.isReadOnly = true;
      editorcontainer.current.documentEditor.showRevisions = false;
      const downloadButton = document.getElementById("documenteditor-share");
      if (downloadButton) {
        downloadButton.style.display = "none";
      }
      //Version-history mode is read-only: hide the Save button on the main title bar.
      if (titleBar) {
        titleBar.setSaveVisible(false);
      }
      setEditorDialogOpen(true);
    }
  };
  function compareSelected(args) {   
    if (downloadButton) {
      (downloadButton).style.display = 'none';
    }
    if ((args.nodeData.parentID) != null) {     
      let treeViewRowElement = args.node.querySelector('.e-text-content');      
      downloadButton = treeViewRowElement.querySelector('.e-de-icon-Download');  
      if (downloadButton) {
        downloadButton.style.display = 'block';

        downloadButton.addEventListener('click', () => {         
          showConfirmationDialog();
        });
      }
      let responseData = {
        DocumentName: editorcontainer.current.documentEditor.documentName + '.docx',       
        SelectedVersion: treeObj.current.selectedNodes[0],
      };

      let baseUrl = url + 'CompareSelectedVersionFromS3';
      let httpRequest = new XMLHttpRequest();
      httpRequest.open('POST', baseUrl, true);
      httpRequest.setRequestHeader(
        'Content-Type',
        'application/json;charset=UTF-8'
      );     
      httpRequest.onreadystatechange = () => {
        if (httpRequest.readyState === 4) {
          if (httpRequest.status === 200 || httpRequest.status === 304) {

            let response = JSON.parse(httpRequest.response);
            editorcontainer.current.documentEditor.open((response.sfdt));          
          } 
        }
      };
      httpRequest.send(JSON.stringify(responseData));
    } else {
      treeObj.current.expandAll([args.node]);
    }
  }
  //  Initialize and render Confirm dialog with options
  function showConfirmationDialog() {
    setVisible(true);
  }

  function okClick() {
    downloadDocument();    
    setVisible(false);
    
  }
  function cancelClick() { 
    setVisible(false);   
  }

  const downloadDocument = () => {    
    let documentVersion = treeObj.current.selectedNodes[0];
    if (documentVersion == null) {
      documentVersion = treeObj.current.getTreeData()[0].id;
    }  
    let responseData = {
      DocumentName: editorcontainer.current.documentEditor.documentName + '.docx',
      SelectedVersion: documentVersion,
    };
    let baseUrl = url + 'DownloadFromS3';
    let httpRequest = new XMLHttpRequest();
    httpRequest.open('POST', baseUrl, true);
    httpRequest.setRequestHeader(
      'Content-Type',
      'application/json;charset=UTF-8'
    );
    httpRequest.responseType = 'blob';
    // Set up event listener for the response
    httpRequest.onload = function () {
      if (httpRequest.status === 200) {
        // Handle the response blob here
        let responseData = httpRequest.response;
        // Create a Blob URL for the response data
        let blobUrl = URL.createObjectURL(responseData);
        // Create a link element and trigger the download
        let downloadLink = document.createElement('a');
        downloadLink.href = blobUrl;
        downloadLink.download =
          editorcontainer.current.documentEditor.documentName + '_' + treeObj.current.selectedNodes[0];
        document.body.appendChild(downloadLink);
        downloadLink.click();
        // Cleanup: Remove the link and revoke the Blob URL
        document.body.removeChild(downloadLink);
        URL.revokeObjectURL(blobUrl);
      } else {
        // Handle errors
        console.error('Request failed with status:', httpRequest.status);
      }
    };
   
    httpRequest.send(JSON.stringify(responseData));
  }

  const toolbarClick = (args) => {
    console.log(args.item);
    let text = args.item.text;
    switch (text) {
      case 'Edit Document':
        setEditorDialogOpen(false);
        loadLatestVersion(container.current.documentEditor.documentName + '.docx');
        container.current.documentEditor.enableContextMenu = true;
        container.current.documentEditor.isReadOnly = false;
        container.current.showPropertiesPane = true;
        //Inside version history, keep the classic Toolbar with the original command list.
        container.current.toolbarMode = 'Toolbar';
        container.current.toolbarItems = ['New', 'Open', 'Separator', 'Undo', 'Redo', 'Separator', 'Image', 'Table', 'Hyperlink', 'Bookmark', 'TableOfContents', 'Separator', 'Header', 'Footer', 'PageSetup', 'PageNumber', 'Break', 'InsertFootnote', 'InsertEndnote', 'Separator', 'Find', 'Separator', 'Comments', 'TrackChanges', 'Separator', 'LocalClipboard', 'RestrictEditing', 'Separator', 'FormFields', 'UpdateFields'];
        const downloadButton = document.getElementById("documenteditor-share");
        if (downloadButton) {
          downloadButton.style.display = "block";
        }
        setDialogOpen(true);
        break;
      case 'Save a copy':
        downloadDocument();
        break;
      default:
        setEditorDialogOpen(false);
        break;
    }
  };
  function nodeTemplate(data) {
    return (<div>
     <div style={{ display: 'flex', alignitems: 'center' }}>
          <div className="ename" style={{ marginright: '30px' }}>{data.name}</div>
          <button id="downloadButton" className="e-btn-icon e-de-icon-Download e-de-padding-right e-icon-left" title="Download a copy of the document." data-ripple="true"></button>
        </div>
        <svg width="20" height="20" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">
          <rect x="4" y="4" width="12" height="12" rx="2" fill="gray" />
        </svg>
        <span style={{ verticalAlign: 'super', marginRight: '10px' }}>{data.user}</span>
        <span style={{ verticalAlign: 'super' }}>modified</span>


  </div>);
}
  return (
    <div className="control-pane documenteditor-list-sample">
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '10px 20px' }}>
        <h2 style={{ margin: 0 }}>Documents</h2>
        <div style={{ display: 'flex', alignItems: 'center' }}>
          <span style={{ marginRight: '8px', fontWeight: 500 }}>Current user:</span>
          <span id="currentUserLabel" style={{ marginRight: '20px', fontWeight: 600 }}>{selectedUser}</span>
          <button className="e-btn e-primary" onClick={newDocumentClick} title="Create a new document">
            <span className="e-btn-icon e-icons e-file-new" style={{ marginRight: '5px' }}></span>New Document
          </button>
        </div>
      </div>
      <GridComponent ref={gridInstance} dataSource={documents} commandClick={onCommandClicked}>
        <ColumnsDirective>
          <ColumnDirective headerText='File Name' template={(props) => (<div className="file-name-container">
            <div className="file-name-content">
              <div className="icon-and-text">
                <svg width="30" height="30" viewBox="0 0 30 30" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M3 3C3 1.34315 4.34315 0 6 0H16.7574C17.553 0 18.3161 0.316071 18.8787 0.87868L26.1213 8.12132C26.6839 8.68393 27 9.44699 27 10.2426V27C27 28.6569 25.6569 30 24 30H6C4.34315 30 3 28.6569 3 27V3Z" fill="#4889EF" />
                  <path d="M17.5 11H25V10.5042C25 9.76949 24.7304 9.0603 24.2422 8.51114L19.9463 3.67818C18.9974 2.61074 17.6374 2 16.2092 2H16V9.5C16 10.3284 16.6716 11 17.5 11Z" fill="#D6E5FE" />
                  <path d="M10.3044 12H10.8868H11.104H11.6817L12.6231 16.3922L13.3963 12H15L13.5719 19H12.777H12.5552H11.8943L10.993 15.0093L10.1103 19H9.44945H9.22761H8.42808L7 12H8.60832L9.38188 16.3816L10.3044 12Z" fill="white" />
                  <rect x="7" y="21" width="16" height="2" rx="1" fill="white" />
                  <rect x="7" y="25" width="11" height="2" rx="1" fill="white" />
                </svg>
                <div className="file-name-text">{props.FileName}</div>
              </div>
            </div>
          </div>)} />
          <ColumnDirective headerText='Versions' field='VersionCount' textAlign='Center' width={110}></ColumnDirective>
          <ColumnDirective headerText='Last Modified' field='LastModifiedTime' type='dateTime' format='M/d/yyyy hh:mm' textAlign='Left'></ColumnDirective>
          <ColumnDirective headerText='Actions' commands={commands} textAlign='Center'></ColumnDirective>
        </ColumnsDirective>
        <Inject services={[CommandColumn]} />
      </GridComponent>
      <DialogComponent id="defaultDialog" ref={dialogInstance} isModal={true} visible={isDialogOpen} width={'90%'} height={'90%'} zIndex={1500} open={dialogOpen} close={dialogClose} minHeight={'650px'}>
        <div id="defaultDialogContent" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
          <div id="documenteditor_titlebar" className="e-de-ctn-title"></div>
          <div id="documenteditor_container_body" style={{ display: 'flex', height: '100%' }}>
            <DocumentEditorContainerComponent showPropertiesPane={false} id="container" width='100%' height='100%' ref={container} style={{ display: "block" }} serviceUrl={hostUrl} zIndex={3000} enableToolbar={true} locale="en-US" />
          </div>
        </div>
      </DialogComponent>
      <DialogComponent id="editorDialog" ref={editorDialogInstance} isModal={true} visible={isEditorDialogOpen} width={'90%'} height={'90%'} zIndex={1500} open={editorDialogOpen} close={editorDialogClose} minHeight={'650px'}>
        <div id="editorToolbar">  
        <ToolbarComponent clicked={toolbarClick}>
          <ItemsDirective>
            <ItemDirective
              prefixIcon='e-home icon'
              tooltipText='Home'
              text='Home'
            />
            <ItemDirective
              prefixIcon='e-edit icon'
              tooltipText='Edit the latest version'
              text='Edit Document'
              align='Center'
            />
            <ItemDirective
              prefixIcon='e-save icon'
              tooltipText='Save a copy'
              text='Save a copy'
              align='Center'
            />
            <ItemDirective
              prefixIcon='e-btn-icon e-icons e-close'
              tooltipText='Close version history'
              align='Right'
            />
          </ItemsDirective>
        </ToolbarComponent></div>
        <div id="main" style={{ display: 'flex', width: '100%' }}>
          <div id="sub1" style={{ width: '70%' }}>
            <div>
              <div id="documenteditor_titlebar1" className="e-de-ctn-title"></div>
              <div id="documenteditor_container_body1" style={{ height: '100%' }}>
                <DocumentEditorContainerComponent showPropertiesPane={false} id="editorcontainer" height='780px' ref={editorcontainer} style={{ display: "block" }} serviceUrl={hostUrl} zIndex={3000} enableToolbar={false} locale="en-US" />
              </div>
            </div>            
          </div>
          <div id="sub2" style={{ width: '30%' }}>
            <div style={{ fontSize: '24px', marginLeft: '10px' }}>Version History</div>
             <TreeViewComponent id="treeObj" ref={treeObj} fields={{ dataSource: versionHistoryData, id: 'id', text: 'name', child: 'subChild' }} nodeSelected={compareSelected.bind(this)} cssClass='custom' nodeTemplate={nodeTemplate} />
            </div>
        </div>
      </DialogComponent>
      <div>
        <DialogComponent
          minHeight={'160px'}
          visible={visible}
          isModal={true}
          header='Syncfusion Document Editor'
          content='Do you want to download a copy of this file and work offline?'
          showCloseIcon={true}
          closeOnEscape={true}
          animationSettings={{ effect: 'Zoom' }}
          buttons={[
            { click: okClick, buttonModel: { content: 'OK', isPrimary: true } },
            { click: cancelClick, buttonModel: { content: 'Cancel' } }
          ]}
          width='400px'
          close={() => setVisible(false)}
        />
      </div>
      <div className="new-doc-content" style={{ minHeight: '350px', padding: '20px' }}>
      <DialogComponent
        id="newDocumentDialog"
        visible={isNewDocDialogOpen}
        isModal={true}
        header='Create New Document'
        showCloseIcon={true}
        closeOnEscape={true}
        animationSettings={{ effect: 'Zoom' }}
        width='440px'
        close={() => setNewDocDialogOpen(false)}
        buttons={[
          { click: createNewDocument, buttonModel: { content: 'Create', isPrimary: true } },
          { click: () => setNewDocDialogOpen(false), buttonModel: { content: 'Cancel' } }
        ]}
      >
        <div className="new-doc-content">
          <label htmlFor="newDocumentName" className="new-doc-label">Enter the document name:</label>
          {/*Uncontrolled input: the value is read from the DOM when Create is clicked to avoid stale state.*/}
          <input id="newDocumentName" className="e-input" type="text" defaultValue={newDocName}
            onChange={(e) => setNewDocName(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') { createNewDocument(); } }}
            placeholder="e.g. My Project Notes"
            autoFocus />
        </div>
      </DialogComponent>
      </div>
    </div>);
};
export default DocumentList;

const root = createRoot(document.getElementById('sample'));
root.render(<DocumentList />);