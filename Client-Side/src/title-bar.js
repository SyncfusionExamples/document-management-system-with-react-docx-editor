import { createElement } from '@syncfusion/ej2-base';
import { Button } from '@syncfusion/ej2-buttons';
import { DropDownButton } from '@syncfusion/ej2-splitbuttons';
/**
 * Represents document editor title bar.
 */
export class TitleBar {
    tileBarDiv;
    documentTitle;
    documentTitleContentEditor;
    export;
    close;
    print;
    open;
    save;
    documentEditor;
    isRtl;
    dialogComponent;
     userList;
     userMap;
     saveOnClose;
    constructor(element, docEditor, isShareNeeded, isRtl, dialogComponent,saveOnClose) {
        //initializes title bar elements.
        this.tileBarDiv = element;
        this.documentEditor = docEditor;
        this.isRtl = isRtl;
        this.dialogComponent = dialogComponent;
        this.saveOnClose = saveOnClose;
        this.initializeTitleBar(isShareNeeded);
        this.wireEvents();
    }
    initializeTitleBar = (isShareNeeded) => {
        let downloadText;
        let downloadToolTip;
        let printText;
        let closeToolTip;
        let printToolTip;
        let openText;
        let saveText;
        let saveToolTip;
        let documentTileText;
        
        if (!this.isRtl) {
            downloadText = 'Download';
            downloadToolTip = 'Download this document.';
            printText = 'Print';
            printToolTip = 'Print this document (Ctrl+P).';
            closeToolTip = 'Close this document';
            openText = 'Open';
            saveText = 'Save';
            saveToolTip = 'Save this document to the server.';
            documentTileText = 'Document Name. Click or tap to rename this document.';
        }
        else {
            downloadText = 'تحميل';
            downloadToolTip = 'تحميل هذا المستند';
            printText = 'طباعه';
            printToolTip = 'طباعه هذا المستند (Ctrl + P)';
            openText = 'فتح';
            saveText = 'حفظ';
            saveToolTip = 'حفظ هذا المستند';
            documentTileText = 'اسم المستند. انقر أو اضغط لأعاده تسميه هذا المستند';
        }
        // tslint:disable-next-line:max-line-length
        this.documentTitle = createElement('label', { id: 'documenteditor_title_name', styles: 'font-weight:400;text-overflow:ellipsis;white-space:pre;overflow:hidden;user-select:none;cursor:text' });
        // tslint:disable-next-line:max-line-length
        this.documentTitleContentEditor = createElement('div', { id: 'documenteditor_title_contentEditor', className: 'single-line' });
        this.documentTitleContentEditor.appendChild(this.documentTitle);
        this.tileBarDiv.appendChild(this.documentTitleContentEditor);
        this.documentTitleContentEditor.setAttribute('title', documentTileText);
        let btnStyles = 'float:right;background: transparent;box-shadow:none; font-family: inherit;border-color: transparent;'
            + 'border-radius: 2px;color:inherit;font-size:12px;text-transform:capitalize;height:28px;font-weight:400;margin-top: 2px;';
        // tslint:disable-next-line:max-line-length
        this.close = this.addButton('e-icons e-close e-de-padding-right', "", btnStyles, 'de-close', closeToolTip, false);
        this.print = this.addButton('e-de-icon-Print e-de-padding-right', printText, btnStyles, 'de-print', printToolTip, false);
        this.open = this.addButton('e-de-icon-Open e-de-padding-right', openText, btnStyles, 'de-open', openText, false);
        //Save button triggers the document save to the server (creates a new version in the document folder).
        this.save = this.addButton('e-icons e-save e-de-padding-right', saveText, btnStyles, 'de-save', saveToolTip, false);
        let items = [
            { text: 'Syncfusion Document Text (*.sfdt)', id: 'sfdt' },
            { text: 'Word Document (*.docx)', id: 'word' },
            { text: 'Word Template (*.dotx)', id: 'dotx' },
            { text: 'Plain Text (*.txt)', id: 'txt' },
        ];
        // tslint:disable-next-line:max-line-length
        this.export = this.addButton('e-de-icon-Download e-de-padding-right', downloadText, btnStyles, 'documenteditor-share', downloadToolTip, true, items);
        if (!isShareNeeded) {
            this.export.element.style.display = 'none';
            this.save.element.style.display = 'none';
        }
        else {
            this.open.element.style.display = 'none';
        }
        if (this.dialogComponent == null)
            this.close.element.style.display = 'none';
    };
    
    setTooltipForPopup() {
        // tslint:disable-next-line:max-line-length
        document.getElementById('documenteditor-share-popup').querySelectorAll('li')[0].setAttribute('title', 'Download a copy of this document to your computer as an SFDT file.');
        // tslint:disable-next-line:max-line-length
        document.getElementById('documenteditor-share-popup').querySelectorAll('li')[1].setAttribute('title', 'Download a copy of this document to your computer as a DOCX file.');
        // tslint:disable-next-line:max-line-length
        document.getElementById('documenteditor-share-popup').querySelectorAll('li')[2].setAttribute('title', 'Download a copy of this document to your computer as a DOTX file.');
        // tslint:disable-next-line:max-line-length
        document.getElementById('documenteditor-share-popup').querySelectorAll('li')[3].setAttribute('title', 'Download a copy of this document to your computer as a TXT file.');
    }
    
    wireEvents = () => {
        this.print.element.addEventListener('click', this.onPrint);
        this.close.element.addEventListener('click', this.onClose);
        if (this.save) {
            this.save.element.addEventListener('click', this.onSave);
        }
        this.open.element.addEventListener('click', (e) => {
            if (e.target.id === 'de-open') {
                let fileUpload = document.getElementById('uploadfileButton');
                fileUpload.value = '';
                fileUpload.click();
            }
        });
        this.documentTitleContentEditor.addEventListener('keydown', (e) => {
            if (e.keyCode === 13) {
                e.preventDefault();
                this.documentTitleContentEditor.contentEditable = 'false';
                if (this.documentTitleContentEditor.textContent === '') {
                    this.documentTitleContentEditor.textContent = 'Document1';
                }
            }
        });
        this.documentTitleContentEditor.addEventListener('blur', () => {
            if (this.documentTitleContentEditor.textContent === '') {
                this.documentTitleContentEditor.textContent = 'Document1';
            }
            this.documentTitleContentEditor.contentEditable = 'false';
            this.documentEditor.documentName = this.documentTitle.textContent;
        });
        this.documentTitleContentEditor.addEventListener('click', () => {
            this.updateDocumentEditorTitle();
        });
    };
     addUser =(actionInfos) => {
        if (!(actionInfos instanceof Array)) {
            actionInfos = [actionInfos]
        }
        for (let i = 0; i < actionInfos.length; i++) {
            let actionInfo = actionInfos[i];
            if (userMap[actionInfo.connectionId]) {
                continue;
            }
            let avatar = createElement('div', { className: 'e-avatar e-avatar-xsmall e-avatar-circle', styles: 'margin: 0px 5px', innerHTML: this.constructInitial(actionInfo.currentUser) });
            if (userList) {
                userList.appendChild(avatar);
            }
            userMap[actionInfo.connectionId] = avatar;
        }
    };

     removeUser =(conectionId) => {
        if (this.userMap[conectionId]) {
            if (this.userList) {
                this.userList.removeChild(this.userMap[conectionId]);
            }
            delete this.userMap[conectionId];
        }
    }

     constructInitial =(authorName) => {
        const splittedName = authorName.split(' ');
        let initials = '';
        for (let i = 0; i < splittedName.length; i++) {
            if (splittedName[i].length > 0 && splittedName[i] !== '') {
                initials += splittedName[i][0];
            }
        }
        return initials;
    }



    updateDocumentEditorTitle = () => {
        this.documentTitleContentEditor.contentEditable = 'true';
        this.documentTitleContentEditor.focus();
        window.getSelection().selectAllChildren(this.documentTitleContentEditor);
    };
    // Updates document title.
    updateDocumentTitle = () => {
        if (this.documentEditor.documentName === '') {
            this.documentEditor.documentName = 'Untitled';
        }
        this.documentTitle.textContent = this.documentEditor.documentName;
    };
    // tslint:disable-next-line:max-line-length
    addButton(iconClass, btnText, styles, id, tooltipText, isDropDown, items) {
        let button = createElement('button', { id: id, styles: styles });
        this.tileBarDiv.appendChild(button);
        button.setAttribute('title', tooltipText);
        if (isDropDown) {
            // tslint:disable-next-line:max-line-length
            let dropButton = new DropDownButton({ select: this.onExportClick, items: items, iconCss: iconClass, cssClass: 'e-caret-hide', content: btnText, open: () => { this.setTooltipForPopup(); } }, button);
            return dropButton;
        }
        else {
            let ejButton = new Button({ iconCss: iconClass, content: btnText }, button);
            return ejButton;
        }
    }
    onPrint = () => {
        this.documentEditor.print();
    };
    onClose = () => {
        this.dialogComponent.hide();
    };
    //Saves the document to the server via the callback provided by the application.
    //The application decides how to persist the document (server call, database, etc.).
    onSave = () => {
        if (this.saveHandler) {
            this.saveHandler();
        }
    };
    //View-only modes (eye/preview, version history) disable document editing, so the Save button should be hidden.
    //Edit mode re-enables it.
    setSaveVisible = (visible) => {
        //Use the DOM id directly so the inline style is applied to the actual <button id="de-save">,
        //matching the proven approach used for the download button (documenteditor-share).
        const saveButton = document.getElementById('de-save');
        if (saveButton) {
            saveButton.style.display = visible ? '' : 'none';
        }
        if (this.save && this.save.element) {
            this.save.element.style.display = visible ? '' : 'none';
            if (this.save.element.parentElement) {
                this.save.element.parentElement.style.display = visible ? '' : 'none';
            }
        }
    };
    onExportClick = (args) => {
        this.s
        let value = args.item.id;
        switch (value) {
            case 'word':
                this.save('Docx');
                break;
            case 'sfdt':
                this.save('Sfdt');
                break;
            case 'txt':
                this.save('Txt');
                break;
            case 'dotx':
                this.save('Dotx');
                break;
        }
    };
    save = (format) => {
        // tslint:disable-next-line:max-line-length
        this.documentEditor.save(this.documentEditor.documentName === '' ? 'sample' : this.documentEditor.documentName, format);
    };
}
