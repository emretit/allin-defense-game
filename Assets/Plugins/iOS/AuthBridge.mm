#import <AuthenticationServices/AuthenticationServices.h>
#import <CommonCrypto/CommonDigest.h>
#import <Security/Security.h>
#import <UIKit/UIKit.h>

extern "C" void UnitySendMessage(const char *, const char *, const char *);
extern "C" UIViewController *UnityGetGLViewController();

static NSString *DCBase64Url(NSData *data) {
    return [[[data base64EncodedStringWithOptions:0] stringByReplacingOccurrencesOfString:@"+" withString:@"-"]
        stringByReplacingOccurrencesOfString:@"/" withString:@"_"];
}
static NSString *DCRandom(void) {
    uint8_t bytes[32];
    if (SecRandomCopyBytes(kSecRandomDefault, sizeof(bytes), bytes) != errSecSuccess) return nil;
    return [DCBase64Url([NSData dataWithBytes:bytes length:sizeof(bytes)]) stringByReplacingOccurrencesOfString:@"=" withString:@""];
}
static NSString *DCForm(NSDictionary<NSString *, NSString *> *parameters) {
    NSMutableArray *pairs = [NSMutableArray array];
    NSCharacterSet *allowed = [NSCharacterSet characterSetWithCharactersInString:@"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~"];
    for (NSString *key in parameters) [pairs addObject:[NSString stringWithFormat:@"%@=%@", key,
        [parameters[key] stringByAddingPercentEncodingWithAllowedCharacters:allowed]]];
    return [pairs componentsJoinedByString:@"&"];
}
static NSDictionary *DCClaims(NSString *token) {
    NSArray *parts = [token componentsSeparatedByString:@"."];
    if (parts.count != 3) return nil;
    NSString *payload = [[parts[1] stringByReplacingOccurrencesOfString:@"-" withString:@"+"] stringByReplacingOccurrencesOfString:@"_" withString:@"/"];
    while (payload.length % 4) payload = [payload stringByAppendingString:@"="];
    NSData *data = [[NSData alloc] initWithBase64EncodedString:payload options:0];
    if (!data) return nil;
    id claims = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
    return [claims isKindOfClass:NSDictionary.class] ? claims : nil;
}

@interface DCAuthBridge : NSObject <ASAuthorizationControllerDelegate, ASAuthorizationControllerPresentationContextProviding,
    ASWebAuthenticationPresentationContextProviding>
@property(nonatomic, copy) NSString *receiver;
@property(nonatomic, copy) NSString *requestId;
@property(nonatomic, copy) NSString *state;
@property(nonatomic, copy) NSString *nonce;
@property(nonatomic, copy) NSString *verifier;
@property(nonatomic, copy) NSString *clientId;
@property(nonatomic, copy) NSString *redirect;
@property(nonatomic, strong) ASAuthorizationController *apple;
@property(nonatomic, strong) ASWebAuthenticationSession *web;
@property(nonatomic, strong) NSURLSessionDataTask *exchange;
- (void)finish:(NSInteger)status token:(NSString *)token name:(NSString *)name;
- (void)startApple;
- (void)startGoogle:(NSString *)clientId;
@end
static DCAuthBridge *active;

@implementation DCAuthBridge
- (ASPresentationAnchor)presentationAnchorForAuthorizationController:(ASAuthorizationController *)controller {
    return UnityGetGLViewController().view.window;
}
- (ASPresentationAnchor)presentationAnchorForWebAuthenticationSession:(ASWebAuthenticationSession *)session {
    return UnityGetGLViewController().view.window;
}
- (void)finish:(NSInteger)status token:(NSString *)token name:(NSString *)name {
    if (active != self || !self.requestId) return;
    NSString *receiver = self.receiver;
    NSDictionary *result = @{ @"requestId": self.requestId, @"status": @(status), @"token": token ?: @"", @"displayName": name ?: @"" };
    NSData *data = [NSJSONSerialization dataWithJSONObject:result options:0 error:nil];
    NSString *json = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
    self.requestId = nil; self.receiver = nil; self.state = nil; self.nonce = nil; self.verifier = nil;
    self.clientId = nil; self.redirect = nil; self.apple = nil; self.web = nil; self.exchange = nil;
    active = nil;
    if (json) UnitySendMessage(receiver.UTF8String, "OnNativeSignIn", json.UTF8String);
}
- (void)startApple {
    self.state = DCRandom();
    if (!self.state) { [self finish:2 token:nil name:nil]; return; }
    ASAuthorizationAppleIDRequest *request = [[[ASAuthorizationAppleIDProvider alloc] init] createRequest];
    request.requestedScopes = @[ASAuthorizationScopeFullName];
    request.state = self.state;
    self.apple = [[ASAuthorizationController alloc] initWithAuthorizationRequests:@[request]];
    self.apple.delegate = self;
    self.apple.presentationContextProvider = self;
    [self.apple performRequests];
}
- (void)authorizationController:(ASAuthorizationController *)controller didCompleteWithAuthorization:(ASAuthorization *)authorization {
    if (controller != self.apple) return;
    if (![authorization.credential isKindOfClass:ASAuthorizationAppleIDCredential.class]) { [self finish:2 token:nil name:nil]; return; }
    ASAuthorizationAppleIDCredential *credential = authorization.credential;
    NSString *token = [[NSString alloc] initWithData:credential.identityToken encoding:NSUTF8StringEncoding];
    if (!token.length || ![credential.state isEqualToString:self.state]) { [self finish:2 token:nil name:nil]; return; }
    NSString *name = credential.fullName ? [NSPersonNameComponentsFormatter localizedStringFromPersonNameComponents:credential.fullName
        style:NSPersonNameComponentsFormatterStyleDefault options:0] : nil;
    [self finish:0 token:token name:name];
}
- (void)authorizationController:(ASAuthorizationController *)controller didCompleteWithError:(NSError *)error {
    if (controller != self.apple) return;
    BOOL cancelled = [error.domain isEqualToString:ASAuthorizationErrorDomain] && error.code == ASAuthorizationErrorCanceled;
    [self finish:cancelled ? 1 : 2 token:nil name:nil];
}
- (void)startGoogle:(NSString *)clientId {
    if (!clientId.length) { [self finish:3 token:nil name:nil]; return; }
    if (![clientId hasSuffix:@".apps.googleusercontent.com"]) { [self finish:2 token:nil name:nil]; return; }
    self.clientId = clientId;
    NSString *prefix = [clientId substringToIndex:clientId.length - @".apps.googleusercontent.com".length];
    NSString *scheme = [@"com.googleusercontent.apps." stringByAppendingString:prefix];
    self.redirect = [scheme stringByAppendingString:@":/oauthredirect"];
    self.state = DCRandom(); self.nonce = DCRandom(); self.verifier = DCRandom();
    if (!self.state || !self.nonce || !self.verifier) { [self finish:2 token:nil name:nil]; return; }
    NSData *verifierData = [self.verifier dataUsingEncoding:NSUTF8StringEncoding];
    unsigned char digest[CC_SHA256_DIGEST_LENGTH];
    CC_SHA256(verifierData.bytes, (CC_LONG)verifierData.length, digest);
    NSString *challenge = [DCBase64Url([NSData dataWithBytes:digest length:sizeof(digest)]) stringByReplacingOccurrencesOfString:@"=" withString:@""];
    NSString *query = DCForm(@{ @"client_id": clientId, @"redirect_uri": self.redirect, @"response_type": @"code",
        @"scope": @"openid profile", @"state": self.state, @"nonce": self.nonce, @"code_challenge": challenge, @"code_challenge_method": @"S256" });
    NSURL *url = [NSURL URLWithString:[@"https://accounts.google.com/o/oauth2/v2/auth?" stringByAppendingString:query]];
    __weak DCAuthBridge *weakSelf = self;
    self.web = [[ASWebAuthenticationSession alloc] initWithURL:url callbackURLScheme:scheme completionHandler:^(NSURL *callback, NSError *error) {
        dispatch_async(dispatch_get_main_queue(), ^{
            DCAuthBridge *bridge = weakSelf;
            if (!bridge || active != bridge) return;
            if (error) {
                BOOL cancelled = [error.domain isEqualToString:ASWebAuthenticationSessionErrorDomain] && error.code == ASWebAuthenticationSessionErrorCodeCanceledLogin;
                [bridge finish:cancelled ? 1 : 2 token:nil name:nil]; return;
            }
            NSURLComponents *components = [NSURLComponents componentsWithURL:callback resolvingAgainstBaseURL:NO];
            NSURLComponents *expected = [NSURLComponents componentsWithString:bridge.redirect];
            if (![components.scheme isEqualToString:expected.scheme] || ![components.path isEqualToString:expected.path] || components.host.length) {
                [bridge finish:2 token:nil name:nil]; return;
            }
            NSMutableDictionary *parameters = [NSMutableDictionary dictionary];
            for (NSURLQueryItem *item in components.queryItems) {
                if (parameters[item.name]) { [bridge finish:2 token:nil name:nil]; return; }
                parameters[item.name] = item.value ?: @"";
            }
            if (![parameters[@"state"] isEqualToString:bridge.state]) { [bridge finish:2 token:nil name:nil]; return; }
            if (parameters[@"error"]) { [bridge finish:[parameters[@"error"] isEqualToString:@"access_denied"] ? 1 : 2 token:nil name:nil]; return; }
            NSString *code = parameters[@"code"];
            if (!code.length) { [bridge finish:2 token:nil name:nil]; return; }
            [bridge exchangeCode:code];
        });
    }];
    self.web.presentationContextProvider = self;
    self.web.prefersEphemeralWebBrowserSession = NO;
    if (![self.web start]) [self finish:2 token:nil name:nil];
}
- (void)exchangeCode:(NSString *)code {
    NSMutableURLRequest *request = [NSMutableURLRequest requestWithURL:[NSURL URLWithString:@"https://oauth2.googleapis.com/token"]];
    request.HTTPMethod = @"POST"; request.timeoutInterval = 30;
    [request setValue:@"application/x-www-form-urlencoded" forHTTPHeaderField:@"Content-Type"];
    request.HTTPBody = [DCForm(@{ @"client_id": self.clientId, @"redirect_uri": self.redirect, @"grant_type": @"authorization_code",
        @"code": code, @"code_verifier": self.verifier }) dataUsingEncoding:NSUTF8StringEncoding];
    __weak DCAuthBridge *weakSelf = self;
    self.exchange = [NSURLSession.sharedSession dataTaskWithRequest:request completionHandler:^(NSData *data, NSURLResponse *response, NSError *error) {
        dispatch_async(dispatch_get_main_queue(), ^{
            DCAuthBridge *bridge = weakSelf;
            if (!bridge || active != bridge) return;
            if (error || ![response isKindOfClass:NSHTTPURLResponse.class] || ((NSHTTPURLResponse *)response).statusCode != 200 || !data) {
                [bridge finish:2 token:nil name:nil]; return;
            }
            id payload = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
            id token = [payload isKindOfClass:NSDictionary.class] ? payload[@"id_token"] : nil;
            NSDictionary *claims = [token isKindOfClass:NSString.class] ? DCClaims(token) : nil;
            NSString *issuer = claims[@"iss"];
            BOOL validIssuer = [issuer isEqual:@"https://accounts.google.com"] || [issuer isEqual:@"accounts.google.com"];
            // UGS verifies the token signature; this binds the returned token to this native OAuth request.
            if (!claims || ![claims[@"nonce"] isEqual:bridge.nonce] || ![claims[@"aud"] isEqual:bridge.clientId] || !validIssuer ||
                ![claims[@"exp"] isKindOfClass:NSNumber.class] || [claims[@"exp"] doubleValue] <= NSDate.date.timeIntervalSince1970) {
                [bridge finish:2 token:nil name:nil]; return;
            }
            [bridge finish:0 token:token name:nil];
        });
    }];
    [self.exchange resume];
}
@end

extern "C" void AllinAuthRequest(const char *receiver, const char *requestId, int provider, const char *clientId) {
    NSString *target = receiver ? [NSString stringWithUTF8String:receiver] : nil;
    NSString *identifier = requestId ? [NSString stringWithUTF8String:requestId] : nil;
    NSString *client = clientId ? [NSString stringWithUTF8String:clientId] : @"";
    dispatch_async(dispatch_get_main_queue(), ^{
        if (!target.length || !identifier.length) return;
        if (active) {
            NSData *data = [NSJSONSerialization dataWithJSONObject:@{ @"requestId": identifier, @"status": @1 } options:0 error:nil];
            NSString *json = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
            UnitySendMessage(target.UTF8String, "OnNativeSignIn", json.UTF8String); return;
        }
        active = [[DCAuthBridge alloc] init]; active.receiver = target; active.requestId = identifier;
        if (provider == 2) [active startApple];
        else if (provider == 3) [active startGoogle:client];
        else [active finish:2 token:nil name:nil];
    });
}
extern "C" void AllinAuthClearSession(void) {
    dispatch_async(dispatch_get_main_queue(), ^{
        DCAuthBridge *bridge = active;
        [bridge.web cancel]; [bridge.exchange cancel];
        if (@available(iOS 16.0, *)) [bridge.apple cancel];
        [bridge finish:1 token:nil name:nil];
    });
}
