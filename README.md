# document-management-system-with-react-docx-editor
A document management system built with React, Syncfusion DOCX Editor, and a Dockerized ASP.NET Core Web API. Supports document editing, version history, document comparison, save, downloads, and AWS S3 storage integration.

# Version History Sample

A Word-style document editor with versioning. Documents are stored in **AWS S3**; each save creates a new version (`v1.docx`, `v2.docx`, ...) under a per-document prefix, so users can browse history and compare any version against its predecessor.

---

## How it works

1. The **React client** ([`Client-Side`](Client-Side)) shows a grid of all documents that exist in the S3 bucket.
2. Clicking **View** (eye) opens the latest version read-only. Clicking **Edit** (pencil) opens it in Ribbon mode and the **Save** button uploads a new version to S3 as `v{n}.docx`. Clicking **History** (thumbnail) opens a dialog with a tree of all versions; selecting any version diffs it against its predecessor.
3. The **API server** ([`Server-Side`](Server-Side)) runs inside a Docker container and handles load, save, list, compare, and download against the S3 bucket. There is no local file storage — everything lives in S3.

---

## Prerequisites

- **Docker Desktop** installed and **running** (the Docker daemon must be up before you build — start it from the system tray / Docker Desktop first).
- An **AWS S3 bucket** with read/write access.

---

## Setup

### 1. Configure AWS credentials

Open [`Server-Side/src/ej2-documenteditor-server/appsettings.json`](Server-Side/src/ej2-documenteditor-server/appsettings.json) and fill in your keys:

```json
"AccessKey": "Your Access Key from AWS S3",
"SecretKey": "Your Secret Key from AWS S3",
"BucketName": "Your Bucket name from AWS S3"
"BucketRegion": "us-east-1"
```

### 2. Build the image

From inside the `Server-Side` folder (where the `Dockerfile` lives), and after starting Docker:

```console
docker buildx build --platform linux/amd64 -t docxserver:latest --output type=docker .
```

### 3. Find the new image id

```console
docker images
```

Note the `IMAGE ID` of the `docxserver:latest` row (e.g. `6240fdecf515`).

### 4. Run the container

```console
docker run -d -p 6028:80 6240fdecf515
```

This maps container port `80` to host port `6028`. The API is now available at `http://localhost:6028`.

---

### 5. Point the React client at the API

The React client calls the API at the URL defined in `src/index.js`. Update it to match the port from the previous step:

```javascript
let url = 'http://localhost:6028/api/documenteditor/';
```

Keep the Docker container running while using the React application.

---

### 6. Start the React client

Open another terminal in:

```text
Client-Side/
```

Install dependencies:

```bash
npm install
```

Start the development server:

```bash
npm run start
```

Open the URL shown in the terminal, normally:

```text
http://localhost:3000
```

---

## What flows through which endpoint

| Action | Endpoint |
|---|---|
| List documents | `GetAllDocumentsFromS3` |
| Load latest version | `LoadLatestVersionDocumentFromS3` |
| Save (creates new `v{n}.docx`) | `AutoSaveToS3` (Save button in title bar) |
| Show version history tree | `GetVersionDataFromS3` |
| Compare a version with the previous one | `CompareSelectedVersionFromS3` |
| Download a specific version | `DownloadFromS3` |

All endpoints accept `{ fileName }` (or `{ DocumentName }` for compare/download) and read/write directly to the configured S3 bucket.

## Resources

- **Product page:**   [Syncfusion® React DOCX Editor](https://www.syncfusion.com/docx-editor-sdk/react-docx-editor?utm_source=github&utm_medium=listing&utm_campaign=github-github-documenteditor-examples) 

- **Documentation:**   [Syncfusion® React DOCX Editor - Documentation](https://help.syncfusion.com/document-processing/word/word-processor/react/overview?utm_source=github&utm_medium=listing&utm_campaign=github-github-documenteditor-examples) 

- **Online demo:**   [Syncfusion® React DOCX Editor - Online demo](https://document.syncfusion.com/demos/docx-editor/react/#/tailwind3/document-editor/default?utm_source=github&utm_medium=listing&utm_campaign=github-github-documenteditor-examples) 

## Support and feedback 

For any other queries, reach our [Syncfusion® support team](https://support.syncfusion.com/?utm_source=github&utm_medium=listing&utm_campaign=github-github-documenteditor-examples) or post the queries through the [community forums](https://www.syncfusion.com/forums?utm_source=github&utm_medium=listing&utm_campaign=github-github-documenteditor-examples). 

Request new feature through [Syncfusion® feedback portal](https://www.syncfusion.com/feedback?utm_source=github&utm_medium=listing&utm_campaign=github-github-documenteditor-examples). 

## License

This is a commercial product and requires a paid license for possession or use Syncfusion's licensed software, including this component, is subject to the terms and conditions of [Syncfusion's EULA](https://www.syncfusion.com/license/studio/34.1.29/syncfusion_essential_studio_eula.pdf?utm_source=github&utm_medium=listing&utm_campaign=github-github-documenteditor-examples). You can purchase a licnense [here](https://www.syncfusion.com/sales/products?utm_source=github&utm_medium=listing&utm_campaign=github-github-documenteditor-examples) or start a free 30\-day trial [here](https://www.syncfusion.com/account/manage-trials/start-trials?utm_source=github&utm_medium=listing&utm_campaign=github-github-documenteditor-examples). 