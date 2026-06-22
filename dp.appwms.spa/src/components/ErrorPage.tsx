import { Button } from "./ui/button";

export default function ErrorPage() {
    return (
        <div className="flex flex-col items-center justify-center min-h-screen bg-background">
            <div className="text-center">
                <h1 className="text-9xl font-black text-primary">404</h1>
                <h2 className="text-4xl font-bold tracking-tight mt-4">Page Not Found</h2>
                <p className="text-muted-foreground mt-2 max-w-md mx-auto">
                    Sorry, the page you are looking for doesn't exist or has been moved.
                </p>
                <div className="mt-8">
                    <Button size="lg" onClick={() => window.location.href = "/"}>
                        Go to Dashboard
                    </Button>
                </div>
            </div>
        </div>
    );
}