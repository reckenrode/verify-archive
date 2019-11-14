#!/usr/bin/env ruby
#
# verify-archive.rb
# Copyright © 2019 Randy Eckenrode
#
# This program is distributed under the terms of the MIT license.  You should
# have received a copy of this license with this program.  If you did not, you
# can find a copy of this license online at https://opensource.org/licenses/MIT.
#
# frozen_string_literal: true
# encoding: utf-8

require 'digest'
require 'optparse'
require 'zip'

# Command-line parser for the script
class Options
  attr_reader :archive

  # Parses the provided +args+
  def initialize(args)
    parser = OptionParser.new do |opts|
      opts.banner = 'Usage: verify-archive.rb <archive.zip>'
      opts.on('-h', '--help', 'show this help message') do |h|
        Options.show_help h
      end
    end
    parser.parse!(args)
    Options.show_help parser if args.empty?
    @archive = args[0]
  end

  # Alias for +Options.new+
  def self.parse(args)
    new(args)
  end

  # Displays +msg+ then exits
  def self.show_help(msg)
    puts msg
    exit
  end

end

options = Options.parse ARGV

unless File.exist?(options.archive)
  puts "“#{options.archive}” not found"
  exit(-1)
end

errors =
  begin
    Zip::File.open(options.archive) do |archive|
      MissingFile = Struct.new(:name, :type, :hash)
      HashMismatch = Struct.new(:name, :type, :system_hash, :backup_hash)

      errors = []

      archive.each do |entry|
        path = entry.name.force_encoding(Encoding.find('filesystem'))
        path = path.split(File::SEPARATOR).drop 1
        path = File.join('/', *path)

        sys_hash = (Digest::SHA256.file(path).hexdigest if File.exist?(path))
        bak_hash = Digest::SHA256.hexdigest(entry.get_input_stream.read)


        errors << MissingFile.new(path, :missing, bak_hash) if sys_hash.nil?
        unless sys_hash.nil? || sys_hash == bak_hash
          errors << HashMismatch.new(path, :mismatch, sys_hash, bak_hash)
        end
      end

      errors
    end
  rescue
    puts "Error reading “#{options.archive}”, or it is not a zip archive"
    exit(-1)
  end

if errors.empty?
  puts 'All files matched!'
else
  first_time = true
  errors.group_by { |error| File.dirname(error.name) }.each do |path, err_list|
    if first_time
      first_time = false
    else
      puts ''
    end
    puts path
    err_list.sort_by { |error| File.basename(error.name) }.each do |error|
      case error.type
      when :missing
        puts "- “#{File.basename(error.name)}” missing, #{error.hash[0..7]}"
      when :mismatch
        puts(
          "- “#{File.basename(error.name)}” mismatch, "\
          "#{error.system_hash[0..7]} ≠ #{error.backup_hash[0..7]}"
        )
      end
    end
  end
end

