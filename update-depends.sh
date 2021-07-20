#!/bin/sh
scriptpath=$(cd `dirname $0`; pwd)
exec nix shell --inputs-from . nixpkgs#ruby_3_0 -c \
  /bin/sh -c "cd $scriptpath; tail -n +5 ./update-depends.sh | ruby"
#!/usr/bin/env ruby
require 'digest'
require 'json'
require 'net/http'

PACKAGES = {
  src: 'src/VerifyArchive/packages.lock.json',
#  tests: 'tests/VerifyArchive.Tests/packages.lock.json',
}

class Redirection < StandardError
  attr_reader :uri
  def initialize(uri)
    @uri = URI.parse(uri)
  end
end

def get_dependencies(lockfile)
  JSON.parse(File.open(lockfile, &:read))['dependencies'].values[0]
    .filter {|dep, details| details['type'] != 'Project'}
    .transform_values {|details| {version: details['resolved']}}
end

def calculate_hash(package, version)
  uri ||= URI.parse("https://www.nuget.org/api/v2/package/#{package}/#{version}")
  Net::HTTP.start(uri.host, uri.port, use_ssl: uri.scheme == 'https') do |http|
    path = uri.path
    path += "?#{uri.query}" if uri.query
    path += "\##{uri.fragment}" if uri.fragment

    StringIO.open('b') do |file|
      http.request_get(path) do |request|
        case request
          in Net::HTTPRedirection | Net::HTTPFound => redir then
            raise Redirection.new(redir['location'])
          in Net::HTTPOK then
            request.read_body(&file.method(:write))
        end
      end
      "sha256-#{Digest(:SHA256).base64digest(file.string)}"
    end
  end
rescue Redirection => redir
  uri = redir.uri
  retry
end

PACKAGES.each do |name, lockfile|
  deps = get_dependencies(lockfile)
    .map do |package, details|
      [package, {**details, hash: calculate_hash(package, details[:version])}]
    end
    .to_h
  File.open("#{name}-deps.json", 'w') do |file|
    output = JSON.pretty_generate(deps)
    file.write(output)
  end
end
