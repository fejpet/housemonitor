// Minimal no-op stub for legacy X11 Session Management (libICE.so.6 / libSM.so.6)
// Used as a fallback on WSL2/Linux environments where libice6/libsm6 packages are not installed (REQ-020).

int IceAddConnectionWatch(void* watchProc, void* clientData) {
    (void)watchProc;
    (void)clientData;
    return 0;
}

void IceRemoveConnectionWatch(void* watchProc, void* clientData) {
    (void)watchProc;
    (void)clientData;
}

int IceProcessMessages(void* iceConn, void* replyWait, void* replyReadyRet) {
    (void)iceConn;
    (void)replyWait;
    (void)replyReadyRet;
    return 0;
}

void* IceSetIOErrorHandler(void* handler) {
    (void)handler;
    return 0;
}

void* IceSetErrorHandler(void* handler) {
    (void)handler;
    return 0;
}

void* SmcOpenConnection(
    char* networkIdsList,
    void* context,
    int xsmpMajorRev,
    int xsmpMinorRev,
    unsigned long mask,
    void* callbacks,
    char* previousId,
    char** clientIdRet,
    int errorLength,
    char* errorStringRet) {
    (void)networkIdsList;
    (void)context;
    (void)xsmpMajorRev;
    (void)xsmpMinorRev;
    (void)mask;
    (void)callbacks;
    (void)previousId;
    (void)clientIdRet;
    (void)errorLength;
    (void)errorStringRet;
    return 0;
}

int SmcCloseConnection(void* smcConn, int count, char** reasonMsgs) {
    (void)smcConn;
    (void)count;
    (void)reasonMsgs;
    return 0;
}

int SmcInteractRequest(void* smcConn, int dialogType, void* interactProc, void* clientData) {
    (void)smcConn;
    (void)dialogType;
    (void)interactProc;
    (void)clientData;
    return 0;
}

void SmcInteractDone(void* smcConn, int cancelShutdown) {
    (void)smcConn;
    (void)cancelShutdown;
}

void SmcSaveYourselfDone(void* smcConn, int success) {
    (void)smcConn;
    (void)success;
}

void* SmcGetIceConnection(void* smcConn) {
    (void)smcConn;
    return 0;
}

void* SmcSetErrorHandler(void* handler) {
    (void)handler;
    return 0;
}

