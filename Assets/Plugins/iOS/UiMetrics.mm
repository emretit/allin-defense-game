#import <UIKit/UIKit.h>

extern "C" double AllinUiLogicalScreenWidth() { return UIScreen.mainScreen.bounds.size.width; }
extern "C" double AllinUiLogicalScreenHeight() { return UIScreen.mainScreen.bounds.size.height; }
