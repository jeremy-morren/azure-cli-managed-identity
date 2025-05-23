#!/bin/bash

if [ "$REQUEST_URI" = "/NotFound" ]; then
    echo "Error message to STDERROR" 1>&2
    
    echo "Status: 404 Not Found"
    echo ""
    echo "Returning 404 from CGI"
    exit 0
fi

echo "Content-Type: text/plain"
echo ""
env -0 | sort -z | tr '\0' '\n'