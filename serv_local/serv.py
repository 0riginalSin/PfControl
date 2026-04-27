# Primary_Key PF_Bar_Code Imm_Bar_Code Event User_Name Date_Time
import pymysql
import json
import os
import shutil
import sys
from threading import Thread, Event, Lock
from http.server import BaseHTTPRequestHandler, HTTPServer
import time

class Server(BaseHTTPRequestHandler):

        def _set_html_headers(self):
                self.send_response(200)
                self.send_header("Content-type", "text/html")
                self.end_headers()

        #       GET is for clients geting the predi
        def do_GET(self):
                if self.path == '/':
                    global htmlStr
                    self._set_html_headers()
                    mutex.acquire()
                    try:
                        self.wfile.write(htmlStr.encode("1251"))
                    finally:
                        mutex.release()

                elif self.path == '/favicon.ico':
                    self.send_response(200)
                    self.send_header('Content-Type', 'image/x-icon')
                    if os.path.exists( os.path.join(ROOT_PATH, FAVICON_NAME) ):
                        self.end_headers()
                        with open(FAVICON_NAME, "rb") as content:
                            shutil.copyfileobj(content, self.wfile)
                    else:
                        self.send_header('Content-Length', 0)
                        self.end_headers()

        #       POST is for submitting data.
        def do_POST(self):

                print( "incomming http: ", self.path )

                content_length = int(self.headers['Content-Length']) # <--- Gets the size of data
                post_data = self.rfile.read(content_length) # <--- Gets the data itself
                self.send_response(200)

                client.close()
# Execute sql string with MySQL server
def sqlExec(sqlStr):
    try:
        connection = pymysql.connect(host='sgt-server.bwf.ru', user='tester', password='tester', database='test')
        with connection:
            with connection.cursor() as cursor:
                cursor.execute(sqlStr)
        return cursor
    except:
        return pymysql.NULL
# Add html column in the end of [where] string
def addColumn(where, what, alarm=0):
    if(alarm == 0):
        return where+"<th>"+what+"</th>"
    elif(alarm == 1):
        return where+"<th style=\"background-color: #ffff00;\" >"+what+"</th>"
    elif(alarm == 2):
        return where+"<th style=\"background-color: #e40a27;\">"+what+"</th>"
# Add html row in the end of [where] string
def addRow(where, what):
    return where+"<tr>"+what+"</tr>"
# Worker, supposed to execute in secondary thread. Get info from MySQL server table, save it
# in local file; generates htmStr, using this info
def BDandFilesWorker():
    global iniInfo
    global htmlStr
    e = Event()
    dbConnectionOk = False
    while True:
        if thredStop:
            break
        cursor = sqlExec("SELECT Pf_Bar_Code, COUNT( Pf_Bar_Code ) AS num FROM t_ftry \
        WHERE Event = 'Смыкание пресс-формы' GROUP BY Pf_Bar_Code ORDER BY num DESC")
        if thredStop:
            break
        # save in iniInfo and write to local ini-file
        if(cursor != pymysql.NULL): # if successfully recieve info from db
            dbConnectionOk = True
            iniInfo.clear()
            for i in cursor:
                iniInfo[i[0]] = i[1]
            with open(SETTINGS_FILE_NAME, 'w') as iniFile:
                json.dump(iniInfo, iniFile)
        else: # if no connection to db
            dbConnectionOk = False
            if os.path.exists( os.path.join(ROOT_PATH, SETTINGS_FILE_NAME) ):
                with open(SETTINGS_FILE_NAME, 'r') as iniFile:
                    iniInfo = json.load(iniFile)
        # generate htmlStrTemp
        Insides = ""
        if os.path.exists( os.path.join(ROOT_PATH, HTML_FILE_NAME) ):
            with open(HTML_FILE_NAME, 'r', encoding='utf8') as htmlIniFile:
                htmlStrTemp = htmlIniFile.read()
        for i in iniInfo:
            tempRow = ""
            warningLevel = [0, 0]
            tempRow = addColumn(tempRow, i)
            tempRow = addColumn(tempRow, str(iniInfo[i]))
            if( iniInfo[i] >= RESOURCE_BEFORE_A_MEDIUM_REPAIR ):
                warningLevel[0] = 2
            elif( iniInfo[i] >= RESOURCE_BEFORE_A_MEDIUM_REPAIR_WARNING_LIMIT ):
                warningLevel[0] = 1
            tempRow = addColumn(tempRow, str (RESOURCE_BEFORE_A_MEDIUM_REPAIR - iniInfo[i] ), warningLevel[0])
            if( iniInfo[i] >= RESOURCE_BEFORE_OVERHAUL ):
                warningLevel[1] = 2
            elif( iniInfo[i] >= RESOURCE_BEFORE_OVERHAUL_WARNING_LIMIT ):
                warningLevel[1] = 1
            tempRow = addColumn(tempRow, str( RESOURCE_BEFORE_OVERHAUL - iniInfo[i] ), warningLevel[1])
            Insides = addRow(Insides, tempRow)

        htmlStrTemp = htmlStrTemp.replace("&tableInsides", Insides)
        if(dbConnectionOk):
            htmlStrTemp = htmlStrTemp.replace("&dBinfo", "Из базы данных")
        else:
            htmlStrTemp = htmlStrTemp.replace("&dBinfo", "Из резервного файла")
        mutex.acquire()
        # htmlStr setup
        try:
            htmlStr = htmlStrTemp[:]
        finally:
            mutex.release()
            del htmlStrTemp
        if thredStop:
            break
        e.wait(THREAD_SLEEP_TIME)

if __name__ == '__main__':
    RESOURCE_BEFORE_A_MEDIUM_REPAIR = 24
    RESOURCE_BEFORE_A_MEDIUM_REPAIR_WARNING_LIMIT = 18
    RESOURCE_BEFORE_OVERHAUL = 32
    RESOURCE_BEFORE_OVERHAUL_WARNING_LIMIT = 26
    hostName = ""
    hostPort = 8000
    htmlStr = ""

    iniInfo = { }
    ROOT_PATH = os.path.dirname( os.path.abspath(__file__) )
    SETTINGS_FILE_NAME = "ini.json"
    HTML_FILE_NAME = "page.html"
    FAVICON_NAME = "favicon.ico"
    START_ROW = 0
    LIMIT_OF_ROWS = 1000

    THREAD_SLEEP_TIME = 1 # in seconds
    thredStop = False
    mutex = Lock()
    
    # read from local file in iniInfo
    if os.path.exists( os.path.join(ROOT_PATH, SETTINGS_FILE_NAME) ):
        with open(SETTINGS_FILE_NAME, 'r') as iniFile:
            iniInfo = json.load(iniFile)
    # check if local html-file exists
    if os.path.exists( os.path.join(ROOT_PATH, HTML_FILE_NAME) ):
        pass
    else:
        sys.exit( "No %s file found in %s root folder" % (HTML_FILE_NAME, os.path.basename(__file__)) )
    # init and start second thread
    BDandFilesWorkerThread = Thread(target=BDandFilesWorker)
    BDandFilesWorkerThread.start()
    # start server worker in main thread
    serverInstance = HTTPServer((hostName, hostPort), Server)
    print(time.asctime(), "Server Starts - %s:%s" % (hostName, hostPort))
    # stop server with KeyboardInterrupt(ctrl + c)
    try:
            serverInstance.serve_forever()
    except KeyboardInterrupt:
            thredStop = True
    serverInstance.server_close()
    print(time.asctime(), "Server Stops - %s:%s" % (hostName, hostPort))
    BDandFilesWorkerThread.join()